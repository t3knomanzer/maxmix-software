#define POLLING_SERIAL

using MaxMix.Framework;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Ports;
using System.Threading;

namespace MaxMix.Services.Communication
{
    public class CommunicationService
    {
        private readonly SynchronizationContext m_MessageContext = SynchronizationContext.Current;
        // We replace messages of the same type, the queue only needs to hold the number of enums in Command, 11 currently, using 16 for space
        private readonly CircularBuffer<KeyValuePair<Command, IMessage>> m_MessageQueue = new CircularBuffer<KeyValuePair<Command, IMessage>>(16);
        private readonly object m_MessageLock = new object();
        private readonly byte[] m_ReadBuffer = new byte[128];
        private readonly MemoryStream m_WriteBuffer = new MemoryStream(128);

        private SerialPort m_SerialPort;
        private Thread m_Thread;
        private bool m_Stopping;

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

        private const int k_ReadTimeout = 20;
        private const int k_WriteTimeout = 20;

        public Action OnDeviceDisconnected;
        public Action OnDeviceConnected;
        public Action<string> OnFirmwareIncompatible;
        public Action<Command, IMessage> OnMessageRecieved;

        public void Start()
        {
            m_Stopping = false;
            m_Thread = new Thread(Update);
            m_Thread.Name = "Communication Service";
            m_Thread.Start();
        }

        public void Stop()
        {
            m_Stopping = true;
            if (m_Thread != null)
            {
                m_Thread.Join();
                m_Thread = null;
            }
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
            lock (m_MessageLock)
            {
                int index = m_MessageQueue.FindIndex(x => x.Key == command);
                if (index >= 0)
                    m_MessageQueue.RemoveAt(index);
                m_MessageQueue.Enqueue(new KeyValuePair<Command, IMessage>(command, message));
            }
            Write(DateTime.Now);
        }

        private void Update()
        {
            while (true)
            {
                var now = DateTime.Now;
                if (m_SerialPort == null)
                {
                    Connect(now);
                    Thread.Sleep(k_DeviceReconnect);
                }
                else
                {
#if POLLING_SERIAL
                    Read(now);
#endif
                    if (now - m_LastMessageWrite > k_DeviceReconnect)
                        Write(now);
                    Disconnect(now);
                    Thread.Sleep(k_PollngInterval);
                }

                if (m_Stopping)
                {
                    TryCloseSerialPort();
                    break;
                }
            }
        }

        private void Connect(DateTime now)
        {
            string[] portNames = null;
            try { portNames = SerialPort.GetPortNames(); }
            catch { }

            if (portNames == null || portNames.Length == 0)
                return;

            foreach (string portName in portNames)
            {
                string firmware = "";
                try
                {
                    AppLogging.DebugLog(nameof(Connect), portName);
                    m_SerialPort = new SerialPort(portName, 115200);
                    m_SerialPort.ReadTimeout = k_ReadTimeout;
                    m_SerialPort.WriteTimeout = k_WriteTimeout;
                    m_SerialPort.Open();
                    m_SerialPort.DiscardInBuffer();
                    m_SerialPort.DiscardOutBuffer();

                    WriteMessage(now, Command.TEST);
                    Thread.Sleep(20);
                    Command command = (Command)m_SerialPort.ReadByte();
                    if (command != Command.TEST)
                        throw new InvalidOperationException($"Firmware Test reply failed. Reply: '{command}' Bytes: '{m_SerialPort.BytesToRead}'");
                    firmware = m_SerialPort.ReadLine().Replace("\r", "");
                    AppLogging.DebugLog(nameof(Connect), command.ToString(), firmware);
                    if (!FirmwareVersions.IsCompatible(firmware))
                        throw new ArgumentException($"Incompatible Firmware: '{firmware}'.");
#if !POLLING_SERIAL
                    m_SerialPort.DataReceived += OnDataReceived;
#endif
                    m_DeviceConnected = true;
#if POLLING_SERIAL
                    m_DeviceReady = false;
#else
                    m_DeviceReady = true;
#endif
                    m_MessageContext.Post(x => OnDeviceConnected?.Invoke(), null);
                    m_LastMessageRead = now;
                    return;
                }
                catch (Exception e)
                {
                    AppLogging.DebugLogException(nameof(Connect), e);
                    TryCloseSerialPort();

                    if (e is ArgumentException) // Incompatible Firmware
                    {
                        m_MessageContext.Post(x => OnFirmwareIncompatible?.Invoke(firmware), null);
                    }
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

            m_DeviceReady = false;
            m_DeviceConnected = false;

            TryCloseSerialPort();

            m_MessageContext.Post(x => OnDeviceDisconnected?.Invoke(), null);
        }

        // Using a template, with a constraint of IMessage allows us to pass the message without boxing reducing garbage generation
        private unsafe void ReadMessage<T>(DateTime now, Command command) where T : unmanaged, IMessage
        {
            int length = 0;
            try
            {
                while (length < sizeof(T))
                    length += m_SerialPort.Read(m_ReadBuffer, length, sizeof(T) - length);

                if (length != sizeof(T))
                    throw new ArgumentException($"Message Length: {length}. Expected Length: {sizeof(T)}.");
            }
            catch (Exception e)
            {
                AppLogging.DebugLogException(nameof(ReadMessage), e);
                Interlocked.Increment(ref m_ErrorCount);
                return;
            }

            T message = new T();
            message.SetBytes(m_ReadBuffer);
            AppLogging.DebugLog(nameof(ReadMessage), command.ToString(), message.ToString());

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
            try { command = (Command)m_SerialPort.ReadByte(); }
            catch (Exception ex)
            {
                AppLogging.DebugLogException(nameof(Read), ex);
                Interlocked.Increment(ref m_ErrorCount);
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
                            AppLogging.DebugLog(nameof(Read), command.ToString(), firmware);
                            m_LastMessageRead = now;
                            m_LastMessageWrite = now;
                        }
                        catch (Exception e)
                        {
                            AppLogging.DebugLogException(nameof(ReadMessage), e);
                            Interlocked.Increment(ref m_ErrorCount);
                            return;
                        }
                    }
                    break;
                case Command.OK:
                    {
                        AppLogging.DebugLog(nameof(Read), command.ToString());
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
                case Command.ERROR:
                case Command.NONE:
                case Command.DEBUG:
                    Interlocked.Increment(ref m_ErrorCount);
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
            AppLogging.DebugLog(nameof(WriteMessage), command.ToString(), message != null ? message.ToString() : "");

            try { m_SerialPort.Write(buffer, 0, length); }
            catch (Exception e)
            {
                AppLogging.DebugLogException(nameof(WriteMessage), e);
                return;
            }
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
