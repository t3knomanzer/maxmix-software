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
        private const int _timeout = 1000;
        private const int _checkPortInterval = 1000;
        private const int _ackTimeout = 500;
        private const int _initialDiscoveryDelay = 200;
        private const int _maxDiscoveryDelay = 2000;
        #endregion

        #region Fields
        private readonly ISerializationService _serializationService;
        private readonly IList<byte> _buffer;

        private string _portName;
        private SerialPort _serialPort;
        private volatile int _discoveryDelay;

        private Thread _thread;
        private SynchronizationContext _synchronizationContext;

        private DateTime _messageLastSent;
        private DateTime _portLastCheck;
        private byte _messageRevision;
        private bool _waitingAck;
        private readonly object _lock = new object();

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
            Debug.WriteLine("CommunicationService Starting");

            _portName = String.Empty;
            _messageRevision = 0;
            _waitingAck = false;
            _discoveryDelay = _initialDiscoveryDelay;

            _thread = new Thread(() => runThread());
            _thread.Start();
        }

        /// <summary>
        /// Properly ends the connection.
        /// </summary>
        public void Stop()
        {
            Debug.WriteLine("CommunicationService Stopping");

            Disconnect();
        }

        /// <summary>
        /// Sends the message using the ISerializationService provided.
        /// </summary>
        /// <param name="message">The message object to send.</param>
        public bool Send(IMessage message)
        {
            lock (_lock)
            {
                try
                {
                    if ((_serialPort != null) && _serialPort.IsOpen)
                    {
                        var messageBytes = _serializationService.Serialize(message, _messageRevision);
                        _serialPort.Write(messageBytes, 0, messageBytes.Length);

                        Debug.WriteLine("Sent message. Type:" + message.GetType() + " Revision: " + _messageRevision);

                        _messageLastSent = DateTime.Now;
                        _waitingAck = true;


                        while (_waitingAck)
                        {
                            if ((DateTime.Now - _messageLastSent).TotalMilliseconds > _ackTimeout)
                            {
                                // The device did not answer in a timely manner.
                                if (_portName != String.Empty)
                                {
                                    // RaiseError only if we were previously connected.
                                    RaiseError("ACK timed out.");
                                }
                                else
                                {
                                    // In discovery mode, simply try again next time.
                                    _waitingAck = false;
                                }
                                _messageRevision++;
                                return false;
                            }
                        }

                        _messageRevision++;
                        return true;
                    }
                    else
                    {
                        RaiseError("Port Disconnected");
                        return false;
                    }
                }
                catch (Exception e)
                {
                    RaiseError(e.Message);
                    return false;
                }
            } /* End Lock */
        }
        #endregion

        #region Private Methods
        private void runThread()
        {
            while (true)
            {
                lock (_lock)
                {
                    if (_serialPort == null)
                    {
                        // ----------------------------------------------
                        // Discovery : Scan all COM ports a MaxMix Device
                        string[] portNames = { };
                        try { portNames = SerialPort.GetPortNames(); }
                        catch { }

                        Debug.WriteLine("Discovery of COM ports with a delay of " + _discoveryDelay + "ms");
                        foreach (var portName in portNames)
                        {
                            try
                            {
                                _serialPort = new SerialPort(portName, _baudRate);
                                _serialPort.ReadTimeout = _timeout;
                                _serialPort.WriteTimeout = _timeout;
                                _serialPort.DataReceived += OnDataReceived;
                                _serialPort.Open();

                                if (_serialPort.IsOpen)
                                {
                                    // Send the initial connection HandShake Request
                                    var message = new MessageHandShakeRequest();
                                    if (Send(message))
                                    {
                                        Debug.WriteLine("MaxMix Device identified on port: " + portName);
                                        _portName = portName;
                                        _portLastCheck = DateTime.Now;
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
                            finally
                            {
                                Thread.Sleep(_discoveryDelay);
                            }
                        }

                        _discoveryDelay = Math.Min(_discoveryDelay + 20, _maxDiscoveryDelay);
                    }
                    else
                    {
                        // ----------------------------------------------
                        // Check if the port is still open
                        if ((DateTime.Now - _portLastCheck).TotalMilliseconds > _checkPortInterval)
                        {
                            _portLastCheck = DateTime.Now;
                            if (!_serialPort.IsOpen)
                            {
                                RaiseError("COM port is no longer open.");
                            }
                        }

                    }

                    // Need to sleep a bit so we don't hog the CPU.
                    Thread.Sleep(1);
                }
            }
        }

        private void Disconnect()
        {
            try
            {
                _serialPort.DataReceived -= OnDataReceived;
                _serialPort.Close();
                _serialPort.Dispose();
                _serialPort = null;
                _portName = String.Empty;
                _discoveryDelay = _initialDiscoveryDelay;
            }
            catch { }
        }

        private void Receive()
        {
            while (_serialPort.BytesToRead > 0)
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
                                {
                                    RaiseError("ACK revision error, received: " + ack.Revision + " expected: " + _messageRevision);
                                }
                            }
                            else
                            {
                                RaiseMessageReceived(message);
                            }
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

        #region EventHandlers
        private void OnDataReceived(object sender, SerialDataReceivedEventArgs e)
        {
            Receive();
        }
        #endregion

        #region Event Dispatchers
        private void RaiseMessageReceived(IMessage message)
        {
            MessageReceived?.Invoke(this, message);
        }

        private void RaiseError(string error)
        {
            Debug.WriteLine("Error Raised: " + error);

            Disconnect();

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
