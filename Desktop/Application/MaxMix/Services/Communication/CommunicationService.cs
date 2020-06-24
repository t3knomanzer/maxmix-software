using System;
using System.Collections.Generic;
using System.IO.Ports;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

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
        private const int _checkPortInterval = 500;
        private const int _timeout = 100;
        #endregion

        #region Fields
        private readonly ISerializationService _serializationService;
        private readonly IList<byte> _buffer;

        private string _portName;
        private int _baudRate;
        private SerialPort _serialPort;

        private Thread _portStateThread;
        private bool _isCheckPortState;
        private SynchronizationContext _synchronizationContext;
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
        #endregion

        #region Public Methods
        /// <summary>
        /// Establishes a connection and begins the communication process.
        /// </summary>
        /// <param name="portName">Name of the COM port to connect to.</param>
        /// <param name="baudRate">Baudrate of the connection.</param>
        public void Start(string portName, int baudRate = 115200)
        {
            _portName = portName;
            _baudRate = baudRate;

            Connect(_portName, _baudRate);

            _isCheckPortState = true;
            _portStateThread = new Thread(() => CheckPortState());
            _portStateThread.Start();
        }

        /// <summary>
        /// Properly ends the connection.
        /// </summary>
        public void Stop()
        {
            _isCheckPortState = false;
            _portStateThread?.Join();

            Disconnect();
        }

        /// <summary>
        /// Sends the message using the ISerializationService provided.
        /// </summary>
        /// <param name="message">The message object to send.</param>
        public void Send(IMessage message)
        {
            try
            {
                var message_ = _serializationService.Serialize(message);
                _serialPort.Write(message_, 0, message_.Length);
            }
            catch(Exception e)
            {
                RaiseError(e.Message);
            }
        }
        #endregion

        #region Private Methods
        private void CheckPortState()
        { 
            while (_isCheckPortState)
            {
                if (!_serialPort.IsOpen)
                {
                    RaiseError("Port closed.");
                    return;
                }
                Thread.Sleep(_checkPortInterval);
            }
        }

        private void Connect(string portName, int baudRate)
        {
            try
            {
                _serialPort = new SerialPort(portName, baudRate);
                _serialPort.ReadTimeout = _timeout;
                _serialPort.WriteTimeout = _timeout;

                _serialPort.Open();
                _serialPort.DataReceived += OnDataReceived;
            }
            catch { RaiseError($"Can't connect to port {portName}"); }
        }

        private void Disconnect()
        {
            try
            {
                _serialPort.DataReceived -= OnDataReceived;
                _serialPort.Close();
                _serialPort.Dispose();
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
                   var message = _serializationService.Deserialize(_buffer.ToArray());
                    if (message != null)
                        RaiseMessageReceived(message);
                    else
                        RaiseError("Deserialization error.");

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
            if(_synchronizationContext != SynchronizationContext.Current)
                _synchronizationContext.Post(o => Error?.Invoke(this, error), null);
            else
                Error?.Invoke(this, error);
        }
        #endregion
    }
}
