using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Ports;
using System.Runtime.InteropServices;

namespace MaxMix.Services.NewCommunication
{
    public class NewCommunicationService
    {
        // We replace messages of the same type, the queue only needs to hold the number of enums in Command, 11 currently, using 16 for space
        private CircularBuffer<KeyValuePair<Command, IMessage>> m_MessageQueue = new CircularBuffer<KeyValuePair<Command, IMessage>>(16);
        private readonly object m_Lock = new object();
        private readonly byte[] m_ReadBuffer = new byte[128];
        private MemoryStream m_WriteBuffer = new MemoryStream(128);

        private SerialPort m_SerialPort;

        // Statistics
        private bool m_DeviceReady;
        private long m_ReadCount;
        private long m_ReadBytes;
        private long m_WriteCount;
        private long m_WriteBytes;
        private long m_ErrorCount;
        private DateTime m_LastMessage;
        private TimeSpan m_DeviceTimeout = new TimeSpan(0, 0, 30);

        public void Start()
        {
            // TODO: start???
        }

        // Usage of Interface on struct causes boxing which then causes garbage, fix this, use templates to pass in explicit message
        // And update m_MessageQueue to use fixed size buffers for the Value
        public void SendMessage(Command command, IMessage message)
        {
            lock (m_Lock)
            {
                int index = m_MessageQueue.FindIndex(x => x.Key == command);
                if (index >= 0)
                    m_MessageQueue.RemoveAt(index);
                m_MessageQueue.Enqueue(new KeyValuePair<Command, IMessage>(command, message));
            }
        }

        void Update()
        {
            // TODO: whiel thread is not wanting to close
            while (true)
            {
                Connect();
                Read();
                Write();
                Disconnect();
            }
        }

        private void Connect()
        {
            if (m_SerialPort != null)
                return;

            string[] portNames = null;
            try { portNames = SerialPort.GetPortNames(); }
            catch { }

            if (portNames == null || portNames.Length == 0)
                return;

            foreach (var portName in portNames)
            {
                try
                {
                    m_SerialPort = new SerialPort(portName, 115200);
                    m_SerialPort.ReadTimeout = 20;
                    m_SerialPort.WriteTimeout = 20;
                    m_SerialPort.Open();

                    WriteMessage(Command.TEST);
                    string firmware = m_SerialPort.ReadLine();
                    // TODO: Actual firmware check
                    if (firmware != "0.0.0")
                        throw new ArgumentException($"Incompatible Firmware: '{firmware}'. Expected: '0.0.0'.");
                    m_LastMessage = DateTime.Now;
                    return;
                }
                catch (Exception e)
                {
                    m_SerialPort.Close();
                    m_SerialPort.Dispose();
                    m_SerialPort = null;

                    if (e is ArgumentException)
                    {
                        Console.WriteLine(e);
                        // TODO: Raise incompatible firmware event
                    }
                }
            }
        }

        private void Disconnect()
        {
            if (m_SerialPort == null)
                return;

            if (DateTime.Now - m_LastMessage < m_DeviceTimeout)
                return;

            m_SerialPort.Close();
            m_SerialPort.Dispose();
            m_SerialPort = null;

            // TODO: Raise device timeout event
        }

        // Using a template, with a constraint of IMessage allows us to pass the message without boxing reducing garbage generation
        private unsafe void ReadMessage<T>(Command command) where T : unmanaged, IMessage
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
                Console.WriteLine($"[Exception]: ReadMessage({command}, {typeof(T).Name}");
                Console.WriteLine(e);
                m_ErrorCount++;
                return;
            }

            T message = new T();
            message.SetBytes(m_ReadBuffer);

            m_ReadBytes += length;
            m_LastMessage = DateTime.Now;

            // TODO: Raise command & message read event
        }

        private void Read()
        {
            if (m_SerialPort == null || !m_SerialPort.IsOpen)
                return;

            if (m_SerialPort.BytesToRead <= 0)
                return;

            Command command = (Command)m_SerialPort.ReadByte();
            m_ReadCount++;
            m_ReadBytes++;
            switch (command)
            {
                case Command.OK:
                    {
                        m_DeviceReady = true;
                        m_LastMessage = DateTime.Now;
                    }
                    break;
                case Command.SETTINGS:
                    ReadMessage<DeviceSettings>(command);
                    break;
                case Command.SESSION_INFO:
                    ReadMessage<SessionInfo>(command);
                    break;
                case Command.CURRENT_SESSION:
                case Command.PREVIOUS_SESSION:
                case Command.NEXT_SESSION:
                case Command.ALTERNATE_SESSION:
                    ReadMessage<SessionData>(command);
                    break;
                case Command.VOLUME_ALT_CHANGE:
                case Command.VOLUME_CHANGE:
                    ReadMessage<VolumeData>(command);
                    break;
                case Command.ERROR:
                case Command.NONE:
                case Command.TEST:
                case Command.DEBUG:
                    m_ErrorCount++;
                    break;
            }
        }

        private void WriteMessage(Command command, IMessage message = null)
        {
            m_WriteBuffer.Position = 0;
            m_WriteBuffer.WriteByte((byte)command);
            message?.GetBytes(m_WriteBuffer);
            m_WriteBytes += (uint)m_WriteBuffer.Length;

            // GetBuffer returns a reference to the underlying array
            byte[] buffer = m_WriteBuffer.GetBuffer();
            try { m_SerialPort.Write(buffer, 0, buffer.Length); }
            catch (Exception e)
            {
                Console.WriteLine($"[Exception]: WriteMessage({command}, {BitConverter.ToString(buffer)}");
                Console.WriteLine(e);
            }
        }

        private void Write()
        {
            if (!m_DeviceReady)
                return;

            KeyValuePair<Command, IMessage> pair;
            lock (m_Lock)
                pair = m_MessageQueue.Dequeue();

            m_DeviceReady = false;
            m_WriteCount++;
            switch (pair.Key)
            {
                case Command.TEST:
                case Command.OK:
                case Command.SETTINGS:
                case Command.SESSION_INFO:
                case Command.CURRENT_SESSION:
                case Command.PREVIOUS_SESSION:
                case Command.NEXT_SESSION:
                case Command.ALTERNATE_SESSION:
                case Command.VOLUME_CHANGE:
                case Command.VOLUME_ALT_CHANGE:
                case Command.DEBUG:
                    WriteMessage(pair.Key, pair.Value);
                    break;
                case Command.ERROR:
                case Command.NONE:
                    {
                        m_DeviceReady = true;
                        m_ErrorCount++;
                    }
                    break;
            }
        }
    }
}
