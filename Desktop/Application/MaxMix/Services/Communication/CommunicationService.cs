using System;
using System.Collections.Generic;
using System.IO.Ports;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Diagnostics;
using System.IO;

namespace MaxMix.Services.Communication
{
    /// <summary>
    /// Manages communication between pc and device including discovery
    /// through handshake as well as messaging.
    /// It is protocol agnostic, it uses the provided message ISerializationService.
    /// </summary>
    internal class CommunicationService : ICommunicationService
    {
        #region Constructor
        public CommunicationService(ISerializationService serializationService)
        {
            _serializationService = serializationService;
        }
        #endregion

        #region Consts
        private const int _baudRate = 115200;
        private const int _portTimeout = 100;
        private const int _ackTimeout = 100;
        private const int _checkPortInterval = 500;
        private const int _sendRetryMax = 3;
        #endregion

        #region Fields
        private readonly SynchronizationContext _synchronizationContext = SynchronizationContext.Current;
        private readonly ISerializationService _serializationService;
        private readonly IList<byte> _buffer = new List<byte>();
        private readonly object _sendLock = new object();

        private SerialPort _serialPort;

        private Thread _reconnectionThread;
        private bool _reconnectionAlive;

        private byte _messageRevision;
        private bool _waitingAck;
        private int _sendRetryCount;

        private Stopwatch _watch = new Stopwatch();
        #endregion

        #region Properties
        #endregion

        #region Events
        /// <inheritdoc/>
        public event EventHandler<IMessage> MessageReceived;

        /// <inheritdoc/>
        public event EventHandler<string> Error;

        /// <inheritdoc/>
        public event EventHandler<string> DeviceDiscovered;
        #endregion

        #region Public Methods
        /// <inheritdoc/>
        public void Start()
        {
            Debug.WriteLine("[CommunicationService] Start");

            _messageRevision = 0;

            _reconnectionAlive = true;
            _reconnectionThread = new Thread(() => HandleReconnection());
            _reconnectionThread.Start();
        }

        /// <inheritdoc/>
        public void Stop()
        {
            Debug.WriteLine("[CommunicationService] Stop");

            _reconnectionAlive = false;
            _reconnectionThread.Join(100);

            try
            {
                _serialPort.DataReceived -= OnDataReceived;
                _serialPort.Close();
                _serialPort.Dispose();
                _serialPort = null;
            }
            catch { }
        }

        /// <inheritdoc/>
        public bool Send(IMessage message)
        {
            lock (_sendLock)
            {
                Debug.WriteLine("[CommunicationService] Send");
                _sendRetryCount = 0;

                while (_sendRetryCount < _sendRetryMax)
                {
                    try
                    {
                        _waitingAck = true;

                        var messageBytes = _serializationService.Serialize(message, _messageRevision);
                        _serialPort.Write(messageBytes, 0, messageBytes.Length);

                        Debug.WriteLine($"[CommunicationService] Message sent: {message.GetType()} Revision: {_messageRevision}");
                        _watch.Restart();
                    }
                    catch (Exception e)
                    {
                        RaiseError(e.Message);
                        return false;
                    }

                    while (_waitingAck)
                    {
                        if (_watch.Elapsed.TotalMilliseconds > _ackTimeout)
                        {
                            Debug.WriteLine($"[CommunicationService] ACK timeout Revision: {_messageRevision}");
                            break;
                        }

                        Thread.Sleep(5);
                    }

                    _messageRevision++;

                    if (!_waitingAck)
                        return true;

                    _sendRetryCount++;
                    Debug.WriteLine($"[CommunicationService] Send retry... {_sendRetryCount}");
                }

                RaiseError($"ACK not received for revision: {_messageRevision}");
                return false;
            }
        }
        #endregion

        #region Private Methods
        /// <summary>
        /// 
        /// </summary>
        private void HandleReconnection()
        {
            while (_reconnectionAlive)
            {
                if (_serialPort != null && !_serialPort.IsOpen)
                {
                    _serialPort = null;
                    RaiseError("COM port is no longer open.");
                }

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
                            _serialPort.ReadTimeout = _portTimeout;
                            _serialPort.WriteTimeout = _portTimeout;
                            _serialPort.DataReceived += OnDataReceived;
                            _serialPort.Open();

                            if (Send(new MessageHandShakeRequest()))
                            {
                                Debug.WriteLine($"[CommunicationService] Device found in port {portName}");
                                RaiseDeviceDiscovered(portName);
                                break;
                            }
                            else
                                throw new IOException($"Port not responding to handshake {portName}");
                        }
                        catch (Exception e)
                        {
                            Debug.WriteLine($"[CommunicationService] {e.Message}");
                            _serialPort.Close();
                            _serialPort.Dispose();
                            _serialPort = null;
                        }
                    }
                }

                Thread.Sleep(_checkPortInterval);
            }
        }
        #endregion

        #region EventHandlers
        /// <summary>
        /// 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="args"></param>
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
                                    Debug.WriteLine($"[CommunicationService] ACK received successfuly: {ack.Revision}");
                                    _waitingAck = false;
                                }
                                else
                                    RaiseError($"ACK revision error, received: {ack.Revision} expected: {_messageRevision}");
                            }
                            else
                                RaiseMessageReceived(message);
                        }
                    }
                    catch (Exception e)
                    {
                        RaiseError("Deserialization error: " + e.Message);
                    }

                    _buffer.Clear();
                }
            }
        }
        #endregion

        #region Event Dispatchers
        /// <summary>
        /// 
        /// </summary>
        /// <param name="message"></param>
        private void RaiseMessageReceived(IMessage message)
        {
            MessageReceived?.Invoke(this, message);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="error"></param>
        private void RaiseError(string error)
        {
            Debug.WriteLine($"[CommunicationService] Error Raised: {error}");

            if (_synchronizationContext != SynchronizationContext.Current)
                _synchronizationContext.Post(o => Error?.Invoke(this, error), null);
            else
                Error?.Invoke(this, error);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="portName"></param>
        private void RaiseDeviceDiscovered(string portName)
        {
            if (_synchronizationContext != SynchronizationContext.Current)
                _synchronizationContext.Post(o => DeviceDiscovered?.Invoke(this, portName), null);
            else
                DeviceDiscovered?.Invoke(this, portName);
        }

        #endregion
    }
}
