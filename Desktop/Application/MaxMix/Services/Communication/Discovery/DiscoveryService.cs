using System;
using System.Collections.Generic;
using System.IO.Ports;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MaxMix.Services.Communication
{
    /// <summary>
    /// Finds the port where a particular device is connected.
    /// Sends a handshake message to all available COM ports until it receives the
    /// expected response from one of them.
    /// </summary>
    internal class DiscoveryService : IDiscoveryService
    {
        #region Constructor
        public DiscoveryService(ISerializationService serializationService)
        {
            _serializationService = serializationService;
        }
        #endregion

        #region Events
        /// <summary>
        /// Raised when a succesful handshake has been established.
        /// </summary>
        public event EventHandler<string> DeviceDiscovered;

        /// <summary>
        /// Raise when an error has been encountered.
        /// </summary>
        public event EventHandler<string> Error;
        #endregion

        #region Consts
        private const int _timeout = 100;
        private const int _handShakeTimeout = 2500;
        private const int _delay = 250;
        #endregion

        #region Fields
        private readonly ISerializationService _serializationService;
        private int _baudRate;
        private bool _isRunning;
        private byte _revision;
        #endregion

        #region Public Methods
        /// <summary>
        /// Begins the scanning process.
        /// </summary>
        /// <param name="baudRate">Expected baudrate for the communication.</param>
        public async void Start(int baudRate = 115200)
        {
            _baudRate = baudRate;
            _isRunning = true;
            _revision = 0;

            string portName = string.Empty;
            while (portName == string.Empty && _isRunning)
            {
                portName = await DiscoverAsync();
                await Task.Delay(_delay);
            }

            RaiseDeviceDiscovered(portName);
        }

        /// <summary>
        /// Stops the discovery process appropiately. 
        /// </summary>
        public void Stop() => _isRunning = false;
        #endregion

        #region Private Methods
        private Task<string> DiscoverAsync()
        {
            return Task.Run<string>(() => Discover());
        }

        private string Discover()
        {
            var portNames = SerialPort.GetPortNames();
            
            var result = string.Empty;
            SerialPort serialPort = null;

            foreach (var portName in portNames)
            {
                try
                {
                    serialPort = new SerialPort(portName, _baudRate);
                    serialPort.ReadTimeout = _timeout;
                    serialPort.WriteTimeout = _timeout;
                    serialPort.Open();

                    Send(serialPort);
                }
                catch
                {
                    if(serialPort != null && serialPort.IsOpen)
                        serialPort.Close();

                    continue;
                }

                try
                {
                    if (Receive(serialPort))
                    {
                        result = portName;
                        break;
                    }
                }
                catch { continue; }
                finally { serialPort.Close(); }

            }

            return result;
        }

        private void Send(SerialPort serialPort)
        {
            var message = _serializationService.Serialize(new MessageHandShakeRequest(), _revision);
            serialPort.Write(message, 0, message.Length);
        }

        private bool Receive(SerialPort serialPort)
        {
            var startTime = DateTime.Now;
            var buffer = new List<byte>();
            IMessage message = null;

            while((DateTime.Now - startTime).TotalMilliseconds < _handShakeTimeout)
            {
                if(serialPort.BytesToRead > 0)
                {
                    byte received = (byte)serialPort.ReadByte();
                    buffer.Add(received);

                    if (received == _serializationService.Delimiter)
                    {
                        try
                        {
                            message = _serializationService.Deserialize(buffer.ToArray());
                            // Here we voluntarily don't check the message's revision, we just want to connect as fast as possible.
                            return message != null && message.GetType() == typeof(MessageAcknowledgment);
                        }
                        catch(ArgumentException) { }
                    }
                }
            }

            return false;
        }
        #endregion

        #region Event Handlers 
        #endregion

        #region Event Methods
        private void RaiseDeviceDiscovered(string portName)
        {
            DeviceDiscovered?.Invoke(this, portName);
        }

        private void RaiseError(string error)
        {
            Error?.Invoke(this, error);
        }
        #endregion
    }
}
