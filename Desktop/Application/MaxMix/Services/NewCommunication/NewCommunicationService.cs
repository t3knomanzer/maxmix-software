using System;
using System.Collections.Generic;
using System.IO.Ports;

namespace MaxMix.Services.NewCommunication
{
    public class NewCommunicationService
    {
        // We replace messages of the same type, the queue only needs to hold the number of enums in Command, 11 currently, using 16 for space
        private CircularBuffer<KeyValuePair<Command, IMessage>> m_MessageQueue = new CircularBuffer<KeyValuePair<Command, IMessage>>(16);
        private readonly object m_Lock = new object();

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
            // TODO: while not wanting to close
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
                        Console.WriteLine(e);

                    // TODO: Raise incompatible firmware event
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

        private void ReadMessage(Command command, IMessage message)
        {
            var buffer = message.GetBytes();

            int length = 0;
            try
            {
                while (length < buffer.Length)
                    length += m_SerialPort.Read(buffer, length, buffer.Length - length);

                if (length != buffer.Length)
                    throw new ArgumentException($"Message Length: {length}. Expected Length: {buffer.Length}.");
            }
            catch (Exception e)
            {
                Console.WriteLine($"[Exception]: ReadMessage({command}, {message.GetType().Name}");
                Console.WriteLine(e);
                m_ErrorCount++;
                return;
            }

            message.SetBytes(buffer);

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
                    ReadMessage(command, Settings.Default());
                    break;
                case Command.SESSION_INFO:
                    ReadMessage(command, SessionInfo.Default());
                    break;
                case Command.CURRENT:
                case Command.PREVIOUS:
                case Command.NEXT:
                    ReadMessage(command, Session.Default());
                    break;
                case Command.VOLUME:
                    ReadMessage(command, Volume.Default());
                    break;
                case Command.SCREEN:
                    ReadMessage(command, Screen.Default());
                    break;
                case Command.TEST:
                case Command.DEBUG:
                default:
                    m_ErrorCount++;
                    break;
            }
        }

        private void WriteMessage(Command command, IMessage message = null)
        {
            byte[] bytes;
            if (message != null)
            {
                byte[] payload = message.GetBytes();
                bytes = new byte[payload.Length + 1];
                Array.Copy(payload, 0, bytes, 1, payload.Length);
            }
            else
            {
                bytes = new byte[1];
            }

            bytes[0] = (byte)command;
            m_WriteBytes += (uint)bytes.Length;

            try { m_SerialPort.Write(bytes, 0, bytes.Length); }
            catch (Exception e)
            {
                Console.WriteLine($"[Exception]: WriteMessage({command}, {BitConverter.ToString(bytes)}");
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
                case Command.CURRENT:
                case Command.PREVIOUS:
                case Command.NEXT:
                case Command.VOLUME:
                case Command.SCREEN:
                case Command.DEBUG:
                    WriteMessage(pair.Key, pair.Value);
                    break;
                default:
                    {
                        m_DeviceReady = true;
                        m_ErrorCount++;
                    }
                    break;
            }
        }
    }
}
