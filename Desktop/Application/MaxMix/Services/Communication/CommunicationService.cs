#define POLLING_SERIAL

using MaxMix.Framework;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Ports;
using System.Threading;

namespace MaxMix.Services.Communication
{
    public class CommunicationService
    {
        private readonly SynchronizationContext m_MessageContext;
        private readonly Func<string[]> m_GetPortNames;
        private readonly Func<string, ISerialConnection> m_CreatePort;
        // We replace messages of the same type, the queue only needs to hold the number of enums in Command, 11 currently, using 16 for space
        private readonly CircularBuffer<KeyValuePair<Command, IMessage>> m_MessageQueue = new CircularBuffer<KeyValuePair<Command, IMessage>>(16);
        private readonly object m_MessageLock = new object();
        private readonly byte[] m_ReadBuffer = new byte[128];
        private readonly MemoryStream m_WriteBuffer = new MemoryStream(128);
        private readonly NLog.Logger m_Logger = NLog.LogManager.GetCurrentClassLogger();

        private ISerialConnection m_SerialPort;
        private Thread m_Thread;
        private volatile bool m_Stopping;
        private volatile bool m_ReconnectRequested;
        private bool m_PausedForFirmware;
        private readonly object m_LifecycleLock = new object();
        private readonly ManualResetEventSlim m_StopSignal = new ManualResetEventSlim();
        private string m_Status;
        private string m_PortName;
        private string m_Firmware;

        // Statistics
        private bool m_DeviceReady;
        private bool m_DeviceConnected;
        private long m_ReadCount;
        private long m_ReadBytes;
        private long m_WriteCount;
        private long m_WriteBytes;
        private long m_ErrorCount;
        private DateTime m_LastMessageRead;
        private DateTime m_LastMessageWrite;
        private readonly TimeSpan k_DeviceTimeout = new TimeSpan(0, 0, 5);
        private readonly TimeSpan k_DeviceReconnect = new TimeSpan(0, 0, 1);
#if POLLING_SERIAL
        private readonly TimeSpan k_PollngInterval = new TimeSpan(0, 0, 0, 0, 10); //d,h,m,s,mil
#else
        private readonly TimeSpan k_PollngInterval = new TimeSpan(0, 0, 1); // h,m,s
#endif

        private const int k_ReadTimeout = 100;
        private const int k_WriteTimeout = 250;

        public Action OnDeviceDisconnected;
        public Action OnDeviceConnected;
        public Action<string> OnFirmwareIncompatible;
        public Action<Command, IMessage> OnMessageRecieved;
        public Action<string, string, string> OnConnectionStatusChanged;
        public bool SupportsButtonSettings { get; private set; }
        public bool SupportsLightingSettings { get; private set; }

        public CommunicationService() : this(SerialPort.GetPortNames, name => new SerialConnection(name),
            SynchronizationContext.Current ?? new SynchronizationContext()) { }

        internal CommunicationService(Func<string[]> getPortNames, Func<string, ISerialConnection> createPort,
            SynchronizationContext context)
        {
            m_GetPortNames = getPortNames;
            m_CreatePort = createPort;
            m_MessageContext = context;
        }

        void OnPowerChange(Object sender, PowerModeChangedEventArgs e)
        {
            switch (e.Mode)
            {
                case PowerModes.Resume:
                    Start();
                    break;
                case PowerModes.Suspend:
                    Stop();
                    break;
                default:
                    break;
            }
        }

        public void Start()
        {
            lock (m_LifecycleLock)
            {
                if (m_PausedForFirmware) return;
                if (m_Thread != null && m_Thread.IsAlive)
                    return;
                m_Stopping = false;
                m_StopSignal.Reset();
                m_Thread = new Thread(Update) { Name = "Communication Service", IsBackground = true };
                m_Thread.Start();
                SystemEvents.PowerModeChanged -= OnPowerChange;
                SystemEvents.PowerModeChanged += OnPowerChange;
            }
        }

        public void Stop()
        {
            lock (m_LifecycleLock)
            {
                m_Stopping = true;
                m_StopSignal.Set();
                if (m_Thread != null)
                    m_Thread.Join();
                m_Thread = null;
            }
        }

        public void Reconnect() => m_ReconnectRequested = true;

        public void PauseForFirmware()
        {
            lock (m_LifecycleLock)
            {
                m_PausedForFirmware = true;
                Stop();
            }
        }

        public void ResumeAfterFirmware()
        {
            lock (m_LifecycleLock)
            {
                m_PausedForFirmware = false;
                Start();
            }
        }

        private void SetStatus(string status, string port = null, string firmware = null)
        {
            if (m_Status == status && m_PortName == port && m_Firmware == firmware)
                return;
            m_Status = status;
            m_PortName = port;
            m_Firmware = firmware;
            m_MessageContext.Post(_ => OnConnectionStatusChanged?.Invoke(status, port, firmware), null);
        }

        public void GetStats(ref long readCount, ref long readBytes, ref long writeCount, ref long writeBytes, ref long errorCount)
        {
            // We update these values via atomics, so we don't need to worry about partial value updates.
            // However, m_ReadCount could be updated, but this call executes before m_ReadBytes is updated
            // So it won't be 100% accurate, however given this is for informational purposes only, this is fine.
            readCount = m_ReadCount;
            readBytes = m_ReadBytes;
            writeCount = m_WriteCount;
            writeBytes = m_WriteBytes;
            errorCount = m_ErrorCount;
        }

        // TODO: Usage of Interface on struct causes boxing which then causes garbage, fix this eventually along with m_MessageQueue
        public void SendMessage(Command command, IMessage message)
        {
            bool valid;
            switch (command)
            {
                case Command.SETTINGS: valid = message is DeviceSettings; break;
                case Command.SESSION_INFO: valid = message is SessionInfo; break;
                case Command.CURRENT_SESSION:
                case Command.ALTERNATE_SESSION:
                case Command.PREVIOUS_SESSION:
                case Command.NEXT_SESSION: valid = message is SessionData; break;
                case Command.VOLUME_CURR_CHANGE:
                case Command.VOLUME_ALT_CHANGE:
                case Command.VOLUME_PREV_CHANGE:
                case Command.VOLUME_NEXT_CHANGE: valid = message is VolumeData; break;
                case Command.MODE_STATES: valid = message is ModeStates; break;
                case Command.BUTTON_SETTINGS: valid = message is ButtonSettings; break;
                case Command.LIGHTING_SETTINGS: valid = message is LightingSettings; break;
                case Command.TEST:
                case Command.OK:
                case Command.DEBUG: valid = message == null; break;
                default: valid = false; break;
            }
            if (!valid)
                throw new ArgumentException("Payload does not match serial command " + command, nameof(message));
            lock (m_MessageLock)
            {
                int index = m_MessageQueue.FindIndex(x => x.Key == command);
                if (index >= 0)
                    m_MessageQueue.RemoveAt(index);
                m_MessageQueue.Enqueue(new KeyValuePair<Command, IMessage>(command, message));
            }
            // Only the communication worker may access the serial port and write buffer.
        }

        private void Update()
        {
            try
            {
                while (!m_Stopping)
                {
                    try
                    {
                        var now = DateTime.UtcNow;
                        if (m_ReconnectRequested)
                        {
                            m_ReconnectRequested = false;
                            ResetConnection("Riconnessione in corso...");
                        }
                        if (m_SerialPort == null)
                        {
                            Connect(now);
                            if (m_SerialPort == null)
                                m_StopSignal.Wait(k_DeviceReconnect);
                        }
                        else
                        {
#if POLLING_SERIAL
                            Read(now);
#endif
                            Write(DateTime.UtcNow);
                            Disconnect(DateTime.UtcNow);
                            m_StopSignal.Wait(k_PollngInterval);
                        }
                    }
                    catch (Exception e)
                    {
                        m_Logger.Warn(e, "Serial connection interrupted");
                        Interlocked.Increment(ref m_ErrorCount);
                        ResetConnection("Connessione interrotta. Nuovo tentativo automatico...");
                        m_StopSignal.Wait(k_DeviceReconnect);
                    }
                }
            }
            finally
            {
                ResetConnection("Connessione sospesa");
            }
        }

        private void Connect(DateTime now)
        {
            string[] portNames = null;
            try { portNames = m_GetPortNames(); }
            catch { }

            if (portNames == null || portNames.Length == 0)
            {
                SetStatus("Nessuna porta USB seriale rilevata. Controlla cavo e collegamento.");
                return;
            }

            foreach (string portName in portNames)
            {
                if (m_Stopping) return;
                string firmware = "";
                try
                {
                    SetStatus("Collegamento a " + portName + "...", portName);
                    m_Logger.Debug(string.Join("\t", nameof(Connect), portName));
                    m_SerialPort = m_CreatePort(portName);
                    m_SerialPort.ReadTimeout = k_ReadTimeout;
                    m_SerialPort.WriteTimeout = k_WriteTimeout;
                    m_SerialPort.Open();
                    m_SerialPort.DiscardInBuffer();
                    m_SerialPort.DiscardOutBuffer();

                    // Nano bootloaders may still be starting after the port opens.
                    // Retry on the same open port rather than restarting the board on every probe.
                    var timer = Stopwatch.StartNew();
                    long nextProbe = 0;
                    while (!m_Stopping && timer.ElapsedMilliseconds < 3000)
                    {
                        if (timer.ElapsedMilliseconds >= nextProbe)
                        {
                            WriteMessage(DateTime.UtcNow, Command.TEST);
                            nextProbe = timer.ElapsedMilliseconds + 250;
                        }
                        try
                        {
                            if (m_SerialPort.ReadByte() != (byte)Command.TEST) continue;
                            firmware = m_SerialPort.ReadLine().Trim();
                            break;
                        }
                        catch (TimeoutException) { }
                    }
                    if (m_Stopping) { TryCloseSerialPort(); return; }
                    if (firmware.Length == 0) throw new TimeoutException("No Maxmix firmware reply");
                    m_Logger.Debug(string.Join("\t", nameof(Connect), firmware));
                    if (!FirmwareVersions.IsCompatible(firmware))
                        throw new ArgumentException($"Incompatible Firmware: '{firmware}'.");
#if !POLLING_SERIAL
                    m_SerialPort.DataReceived += OnDataReceived;
#endif
                    SupportsButtonSettings = FirmwareVersions.SupportsButtonSettings(firmware);
                    SupportsLightingSettings = FirmwareVersions.SupportsLightingSettings(firmware);
                    m_DeviceConnected = true;
#if POLLING_SERIAL
                    m_DeviceReady = false;
#else
                    m_DeviceReady = true;
#endif
                    m_MessageContext.Post(x => OnDeviceConnected?.Invoke(), null);
                    m_LastMessageRead = DateTime.UtcNow;
                    m_Logger.Info("Maxmix connected on {0}, firmware {1}", portName, firmware);
                    SetStatus("Maxmix collegato", portName, firmware);
                    return;
                }
                catch (Exception e)
                {
                    m_Logger.Debug(e, nameof(Connect));
                    TryCloseSerialPort();

                    if (e is ArgumentException) // Incompatible Firmware
                    {
                        m_MessageContext.Post(x => OnFirmwareIncompatible?.Invoke(firmware), null);
                        SetStatus("Firmware " + firmware + " non compatibile", portName, firmware);
                    }
                    else
                        SetStatus("Nessuna risposta Maxmix su " + portName + ". Nuovo tentativo automatico...", portName);
                }
            }
        }

        private void TryCloseSerialPort()
        {
            try
            {
                if (m_SerialPort != null)
                {
#if !POLLING_SERIAL
                    m_SerialPort.DataReceived -= Read;
#endif
                    m_SerialPort.Close();
                    m_SerialPort.Dispose();
                }
            }
            catch { }
            m_SerialPort = null;
        }

        private void Disconnect(DateTime now)
        {
            if (m_SerialPort == null)
                return;

            if (now - m_LastMessageRead < k_DeviceTimeout)
                return;

            ResetConnection("Dispositivo non risponde. Nuovo tentativo automatico...");
        }

        private void ResetConnection(string status)
        {
            bool wasConnected = m_DeviceConnected;
            if (wasConnected) m_Logger.Info(status);
            m_DeviceReady = false;
            m_DeviceConnected = false;
            SupportsButtonSettings = false;
            SupportsLightingSettings = false;
            TryCloseSerialPort();
            lock (m_MessageLock)
            {
                while (m_MessageQueue.Count > 0) m_MessageQueue.Dequeue();
            }
            if (wasConnected) m_MessageContext.Post(_ => OnDeviceDisconnected?.Invoke(), null);
            SetStatus(status);
        }

        // Using a template, with a constraint of IMessage allows us to pass the message without boxing reducing garbage generation
        private unsafe void ReadMessage<T>(DateTime now, Command command) where T : unmanaged, IMessage
        {
            int length = 0;
            try
            {
                while (length < sizeof(T))
                {
                    int read = m_SerialPort.Read(m_ReadBuffer, length, sizeof(T) - length);
                    if (read == 0) throw new EndOfStreamException();
                    length += read;
                }

                if (length != sizeof(T))
                    throw new ArgumentException($"Message Length: {length}. Expected Length: {sizeof(T)}.");
            }
            catch (Exception e)
            {
                m_Logger.Debug(e, nameof(ReadMessage));
                Interlocked.Increment(ref m_ErrorCount);
                m_ReconnectRequested = true;
                return;
            }

            T message = new T();
            message.SetBytes(m_ReadBuffer);
            m_Logger.Debug(string.Join("\t", nameof(ReadMessage), command, message));

            Interlocked.Add(ref m_ReadBytes, length);
            m_LastMessageRead = now;
            m_MessageContext.Post(x => OnMessageRecieved?.Invoke(command, message), null);
        }

        private void Read(DateTime now)
        {
#if POLLING_SERIAL
            try
            {
                if (m_SerialPort == null || !m_SerialPort.IsOpen || m_SerialPort.BytesToRead <= 0)
                    return;
            }
            catch { return; }
#endif

            Command command;
            try { command = (Command)(sbyte)m_SerialPort.ReadByte(); }
            catch (Exception e)
            {
                m_Logger.Debug(e, nameof(Read));
                Interlocked.Increment(ref m_ErrorCount);
                m_ReconnectRequested = true;
                return;
            }

            Interlocked.Increment(ref m_ReadCount);
            Interlocked.Increment(ref m_ReadBytes);
            switch (command)
            {
                case Command.TEST:
                    {
                        try
                        {
                            var firmware = m_SerialPort.ReadLine().Replace("\r", "");
                            m_Logger.Debug(string.Join("\t", nameof(Read), command, firmware));
                            m_LastMessageRead = now;
                            m_LastMessageWrite = now;
                        }
                        catch (Exception e)
                        {
                            m_Logger.Debug(e, nameof(ReadMessage));
                            Interlocked.Increment(ref m_ErrorCount);
                            return;
                        }
                    }
                    break;
                case Command.OK:
                    {
                        m_Logger.Debug(string.Join("\t", nameof(Read), command));
                        m_DeviceReady = true;
                        m_LastMessageRead = now;
                        m_LastMessageWrite = now;
                        Write(m_LastMessageRead);
                    }
                    break;
                case Command.SETTINGS:
                    ReadMessage<DeviceSettings>(now, command);
                    break;
                case Command.SESSION_INFO:
                    ReadMessage<SessionInfo>(now, command);
                    break;
                case Command.CURRENT_SESSION:
                case Command.ALTERNATE_SESSION:
                case Command.PREVIOUS_SESSION:
                case Command.NEXT_SESSION:
                    ReadMessage<SessionData>(now, command);
                    break;
                case Command.VOLUME_CURR_CHANGE:
                case Command.VOLUME_ALT_CHANGE:
                case Command.VOLUME_PREV_CHANGE:
                case Command.VOLUME_NEXT_CHANGE:
                    ReadMessage<VolumeData>(now, command);
                    break;
                case Command.MODE_STATES:
                    ReadMessage<ModeStates>(now, command);
                    break;
                case Command.ERROR:
                case Command.NONE:
                case Command.DEBUG:
                default:
                    Interlocked.Increment(ref m_ErrorCount);
                    m_ReconnectRequested = true;
                    break;
            }
        }

#if !POLLING_SERIAL
        private void OnDataReceived(object sender, SerialDataReceivedEventArgs e)
        {
            if (e.EventType == SerialData.Eof)
                return;

            Read(DateTime.Now);
        }
#endif

        private void WriteMessage(DateTime now, Command command, IMessage message = null)
        {
            m_WriteBuffer.SetLength(0);
            m_WriteBuffer.WriteByte((byte)command);
            message?.GetBytes(m_WriteBuffer);
            Interlocked.Add(ref m_WriteBytes, m_WriteBuffer.Length);

            // GetBuffer returns a reference to the underlying array, we can still use that after we reset the position if we store the length
            byte[] buffer = m_WriteBuffer.GetBuffer();
            int length = (int)m_WriteBuffer.Length;
            m_Logger.Debug(string.Join("\t", nameof(WriteMessage), command, message != null ? message.ToString() : ""));

            m_SerialPort.Write(buffer, 0, length);
            m_LastMessageWrite = now;
        }

        private void Write(DateTime now)
        {
            if (!m_DeviceConnected || !m_DeviceReady)
                return;

            KeyValuePair<Command, IMessage> pair = default;
            if (now - m_LastMessageWrite > k_DeviceReconnect)
            {
                // Send OK test even if we think the device is not ready
                pair = new KeyValuePair<Command, IMessage>(Command.OK, null);
            }
            else if (m_MessageQueue.Count != 0)
            {
                lock (m_MessageLock)
                {
                    if (m_MessageQueue.Count != 0)
                        pair = m_MessageQueue.Dequeue();
                    else
                        return;
                }
            }
            else
            {
                return;
            }

            m_DeviceReady = false;
            Interlocked.Increment(ref m_WriteCount);
            switch (pair.Key)
            {
                case Command.TEST:
                case Command.OK:
                case Command.SETTINGS:
                case Command.SESSION_INFO:
                case Command.CURRENT_SESSION:
                case Command.ALTERNATE_SESSION:
                case Command.PREVIOUS_SESSION:
                case Command.NEXT_SESSION:
                case Command.VOLUME_CURR_CHANGE:
                case Command.VOLUME_ALT_CHANGE:
                case Command.VOLUME_PREV_CHANGE:
                case Command.VOLUME_NEXT_CHANGE:
                case Command.MODE_STATES:
                case Command.BUTTON_SETTINGS:
                case Command.LIGHTING_SETTINGS:
                case Command.DEBUG:
                    WriteMessage(now, pair.Key, pair.Value);
                    break;
                case Command.ERROR:
                case Command.NONE:
                    {
                        m_DeviceReady = true;
                        Interlocked.Increment(ref m_ErrorCount);
                    }
                    break;
            }
        }
    }
}
