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
        private long m_ReadCount;
        private long m_ReadBytes;
        private long m_WriteCount;
        private long m_WriteBytes;
        private long m_ErrorCount;
        private DateTime m_LastMessageRead;
        private DateTime m_LastMessageWrite;
        private TimeSpan m_DeviceTimeout = new TimeSpan(0, 0, 5);
        private TimeSpan m_DeviceReconnect = new TimeSpan(0, 0, 1);

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
            m_Thread.Join();
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
        }

        private void Update()
        {
            while (true)
            {
                Connect();
                Read();
                Write();
                Disconnect();

                if (m_Stopping)
                {
                    if (m_SerialPort != null)
                    {
                        m_SerialPort.Close();
                        m_SerialPort.Dispose();
                        m_SerialPort = null;
                    }
                    break;
                }
            }
        }

        private void Connect()
        {
            if (m_SerialPort != null)
                return;

            if (DateTime.Now - m_LastMessageRead < m_DeviceReconnect)
                return;
            m_LastMessageRead = DateTime.Now;

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

                    WriteMessage(Command.TEST);
                    Thread.Sleep(20);
                    Command command = (Command)m_SerialPort.ReadByte();
                    if (command != Command.TEST)
                        throw new InvalidOperationException($"Firmware Test reply failed. Reply: '{command}' Bytes: '{m_SerialPort.BytesToRead}'");
                    firmware = m_SerialPort.ReadLine().Replace("\r", "");
                    // TODO: Actual firmware check
                    if (firmware != "0.0.0")
                        throw new ArgumentException($"Incompatible Firmware: '{firmware}'. Expected: '0.0.0'.");
                    m_MessageContext.Post(x => OnDeviceConnected?.Invoke(), null);
                    return;
                }
                catch (Exception e)
                {
                    AppLogging.DebugLogException(nameof(Connect), e);
                    m_SerialPort.Close();
                    m_SerialPort.Dispose();
                    m_SerialPort = null;

                    if (e is ArgumentException) // Incompatible Firmware
                    {
                        m_MessageContext.Post(x => OnFirmwareIncompatible?.Invoke(firmware), null);
                    }
                }
            }
        }

        private void Disconnect()
        {
            if (m_SerialPort == null)
                return;

            if (DateTime.Now - m_LastMessageRead < m_DeviceTimeout)
                return;

            m_SerialPort.Close();
            m_SerialPort.Dispose();
            m_SerialPort = null;
            m_LastMessageRead = DateTime.Now;

            m_MessageContext.Post(x => OnDeviceDisconnected?.Invoke(), null);
        }

        // Using a template, with a constraint of IMessage allows us to pass the message without boxing reducing garbage generation
        private unsafe void ReadMessage<T>(Command command) where T : unmanaged, IMessage
        {
            AppLogging.DebugLog(nameof(ReadMessage), command.ToString());
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
                m_ErrorCount++;
                return;
            }

            T message = new T();
            message.SetBytes(m_ReadBuffer);

            Interlocked.Add(ref m_ReadBytes, length);
            m_LastMessageRead = DateTime.Now;

            m_MessageContext.Post(x => OnMessageRecieved?.Invoke(command, message), null);
        }

        private void Read()
        {
            try
            {
                if (m_SerialPort == null || !m_SerialPort.IsOpen || m_SerialPort.BytesToRead <= 0)
                    return;
            }
            catch { return; }

            Command command = (Command)m_SerialPort.ReadByte();
            AppLogging.DebugLog(nameof(Read), command.ToString());
            Interlocked.Increment(ref m_ReadCount);
            Interlocked.Increment(ref m_ReadBytes);
            switch (command)
            {
                case Command.TEST:
                    {
                        var firmware = m_SerialPort.ReadLine().Replace("\r", "");
                        AppLogging.DebugLog("FW:", firmware);
                        m_LastMessageRead = DateTime.Now;
                    }
                    break;
                case Command.OK:
                    {
                        m_DeviceReady = true;
                        m_LastMessageRead = DateTime.Now;
                    }
                    break;
                case Command.SETTINGS:
                    ReadMessage<DeviceSettings>(command);
                    break;
                case Command.SESSION_INFO:
                    ReadMessage<SessionInfo>(command);
                    break;
                case Command.CURRENT_SESSION:
                case Command.ALTERNATE_SESSION:
                case Command.PREVIOUS_SESSION:
                case Command.NEXT_SESSION:
                    ReadMessage<SessionData>(command);
                    break;
                case Command.VOLUME_CURR_CHANGE:
                case Command.VOLUME_ALT_CHANGE:
                case Command.VOLUME_PREV_CHANGE:
                case Command.VOLUME_NEXT_CHANGE:
                    ReadMessage<VolumeData>(command);
                    break;
                case Command.ERROR:
                case Command.NONE:
                case Command.DEBUG:
                    Interlocked.Increment(ref m_ErrorCount);
                    break;
            }
        }

        private void WriteMessage(Command command, IMessage message = null)
        {
            m_WriteBuffer.SetLength(0);
            m_WriteBuffer.WriteByte((byte)command);
            message?.GetBytes(m_WriteBuffer);
            Interlocked.Add(ref m_WriteBytes, m_WriteBuffer.Length);

            // GetBuffer returns a reference to the underlying array, we can still use that after we reset the position if we store the length
            byte[] buffer = m_WriteBuffer.GetBuffer();
            int length = (int)m_WriteBuffer.Length;
            AppLogging.DebugLog(nameof(WriteMessage), command.ToString(), $"{{{ToByteString(buffer, 0, length)}}}");

            try { m_SerialPort.Write(buffer, 0, length); }
            catch (Exception e)
            {
                AppLogging.DebugLogException(nameof(WriteMessage), e);
                return;
            }
            m_LastMessageWrite = DateTime.Now;
        }

        private string ToByteString(byte[] bytes, int offset, int length)
        {
            string msg = "";
            for (int i = offset; i < offset + length; i++)
                msg += bytes[i].ToString("x1");
            return msg;
        }

        private void Write()
        {
            if (!m_DeviceReady || m_SerialPort == null || !m_SerialPort.IsOpen)
                return;

            KeyValuePair<Command, IMessage> pair = default;
            lock (m_MessageLock)
            {
                if (m_MessageQueue.Count != 0)
                    pair = m_MessageQueue.Dequeue();
            }

            if (pair.Key == Command.NONE)
            {
                if (DateTime.Now - m_LastMessageWrite > m_DeviceReconnect)
                    pair = new KeyValuePair<Command, IMessage>(Command.OK, null);
                else
                    return;
            }

            AppLogging.DebugLog(nameof(Write), pair.Key.ToString());
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
                    WriteMessage(pair.Key, pair.Value);
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
