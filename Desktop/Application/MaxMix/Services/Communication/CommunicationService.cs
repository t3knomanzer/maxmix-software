using System;
using System.Collections.Generic;
using System.IO.Ports;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Diagnostics;


namespace MaxMix.Services.Communication
{
    /// <summary>
    /// Manages sending and receiving messages between application and device.
    /// It is protocol agnostic, it uses the provided message ISerializationService.
    /// </summary>
    internal class CommunicationService : ICommunicationService
    {
        #region Constructor
        public CommunicationService(ISerializationService serializationService)
        {
            _serializationService = serializationService;
            _synchronizationContext = SynchronizationContext.Current;
            _buffer = new List<byte>();
        }
        #endregion

        #region Consts
        private const int _baudRate = 115200;
        private const int _timeout = 500;
        private const int _checkPortInterval = 1000;
        private const int _ackTimeout = 500;
        #endregion

        #region Fields
        private readonly SynchronizationContext _synchronizationContext;
        private readonly ISerializationService _serializationService;
        private readonly IList<byte> _buffer;

        private string _portName;
        private SerialPort _serialPort;

        private Thread _reconnectionThread;
        private readonly object _lock = new object();

        private byte _messageRevision;
        private bool _waitingAck;

        private Stopwatch _watch;
        private TimeSpan _messageLastSent;
        #endregion

        #region Properties
        #endregion

        #region Events
        /// <summary>
        /// Raised when a message has been received and deserialized.
        /// </summary>
        public event EventHandler<IMessage> MessageReceived;

        /// <summary>
        /// Raised when an error has happend.
        /// </summary>
        public event EventHandler<string> Error;

        /// <summary>
        /// Raised when a succesful handshake has been established.
        /// </summary>
        public event EventHandler<string> DeviceDiscovered;
        #endregion

        #region Public Methods
        /// <summary>
        /// Attempt to detect the device and begins the communication process.
        /// </summary>
        public void Start()
        {
            Debug.WriteLine("[CommunicationService] Start");

            _portName = String.Empty;
            _messageRevision = 0;
            _waitingAck = false;

            _watch = new Stopwatch();
            _watch.Start();

            _reconnectionThread = new Thread(() => HandleReconnection());
            _reconnectionThread.Start();
        }

        /// <summary>
        /// Properly ends the connection.
        /// </summary>
        public void Stop()
        {
            Debug.WriteLine("[CommunicationService] Stop");

            _reconnectionThread.Abort();
            _watch.Stop();

            Disconnect();
        }

        /// <summary>
        /// Sends the message using the ISerializationService provided.
        /// </summary>
        /// <param name="message">The message object to send.</param>
        public bool Send(IMessage message)
        {
            Debug.WriteLine("[CommunicationService] Send");

            lock (_lock)
            {
                if (_serialPort == null || !_serialPort.IsOpen)
                {
                    RaiseError("Port is closed");
                    return false;
                }

                try
                {
                    var messageBytes = _serializationService.Serialize(message, _messageRevision);
                    _serialPort.Write(messageBytes, 0, messageBytes.Length);

                    Debug.WriteLine($"[CommunicationService] Message sent: {message.GetType()} Revision: {_messageRevision}");
                    _messageLastSent = _watch.Elapsed;
                }
                catch (Exception e)
                {
                    RaiseError(e.Message);
                    return false;
                }

                try
                {
                    _waitingAck = true;
                    while (_waitingAck)
                    {
                        // The device did not answer in a timely manner.
                        if ((_watch.Elapsed - _messageLastSent).TotalMilliseconds > _ackTimeout)
                        {
                            if (_portName != String.Empty)
                            {
                                // RaiseError only if we were previously connected.
                                RaiseError("ACK timed out.");
                            }
                            else
                            {
                                // In discovery mode, simply try again next time.
                                // TODO: We should find a more explicit way of handling this.
                                _waitingAck = false;
                                Debug.WriteLine("[CommunicationService] No handshake received");
                            }

                            _messageRevision++;
                            return false;
                        }

                        Thread.Sleep(5);
                    }

                    _messageRevision++;
                    return true;
                }
                catch (Exception e)
                {
                    RaiseError(e.Message);
                    return false;
                }
            }
        }
        #endregion

        #region Private Methods
        private void HandleReconnection()
        {
            while (true)
            {
                lock (_lock)
                {
                    if (_serialPort == null)
                    {
                        string[] portNames = { };
                        try { portNames = SerialPort.GetPortNames(); }
                        catch { }

                        Debug.WriteLine("[CommunicationService] Discovering devices");

                        foreach (var portName in portNames)
                        {
                            try
                            {
                                Debug.WriteLine($"[CommunicationService] Probing port {portName}");
                                _serialPort = new SerialPort(portName, _baudRate);
                                _serialPort.ReadTimeout = _timeout;
                                _serialPort.WriteTimeout = _timeout;
                                _serialPort.DataReceived += OnDataReceived;
                                _serialPort.Open();

                                if (_serialPort.IsOpen)
                                {
                                    var message = new MessageHandShakeRequest();
                                    if (Send(message))
                                    {
                                        Debug.WriteLine($"[CommunicationService] Device found in port {portName}");
                                        _portName = portName;
                                        RaiseDeviceDiscovered(portName);
                                    }
                                    else
                                    {
                                        _serialPort.Close();
                                        _serialPort.Dispose();
                                        _serialPort = null;
                                    }
                                }
                            }
                            catch { }
                        }

                    }
                    else
                    {
                        if (!_serialPort.IsOpen)
                            RaiseError("COM port is no longer open.");
                    }
                }
                Thread.Sleep(_checkPortInterval);
            }
        }

        private void Disconnect()
        {
            try
            {
                if (_serialPort != null)
                {
                    _serialPort.DataReceived -= OnDataReceived;
                    _serialPort.Close();
                    _serialPort.Dispose();
                    _serialPort = null;
                    _portName = String.Empty;
                    _messageRevision = 0;
                }
            }
            catch { }
        }
        #endregion

        #region EventHandlers
        private void OnDataReceived(object sender, SerialDataReceivedEventArgs args)
        {
            while (_serialPort != null &&  _serialPort.IsOpen && _serialPort.BytesToRead > 0)
            {
                byte received = (byte)_serialPort.ReadByte();
                _buffer.Add(received);

                if (received == _serializationService.Delimiter)
                {
                    try
                    {
                        var message = _serializationService.Deserialize(_buffer.ToArray());
                        if (message != null)
                        {
                            // If it's an ACK message, process it immediately.
                            if (message.GetType() == typeof(MessageAcknowledgment))
                            {
                                MessageAcknowledgment ack = (MessageAcknowledgment)message;
                                if (ack.Revision == _messageRevision)
                                {
                                    Debug.WriteLine("ACK received successfuly: " + ack.Revision);
                                    _waitingAck = false;
                                }
                                else
                                    RaiseError("ACK revision error, received: " + ack.Revision + " expected: " + _messageRevision);
                            }
                            else
                                RaiseMessageReceived(message);
                        }
                    }
                    catch (ArgumentException e)
                    {
                        RaiseError("Deserialization error: " + e.Message);
                    }

                    _buffer.Clear();
                }
            }
        }
        #endregion

        #region Event Dispatchers
        private void RaiseMessageReceived(IMessage message)
        {
            MessageReceived?.Invoke(this, message);
        }

        private void RaiseError(string error)
        {
            Debug.WriteLine($"[CommunicationService] Error Raised: {error}");

            if (_synchronizationContext != SynchronizationContext.Current)
                _synchronizationContext.Post(o => Error?.Invoke(this, error), null);
            else
                Error?.Invoke(this, error);
        }

        private void RaiseDeviceDiscovered(string portName)
        {
            DeviceDiscovered?.Invoke(this, portName);
        }

        #endregion
    }
}
