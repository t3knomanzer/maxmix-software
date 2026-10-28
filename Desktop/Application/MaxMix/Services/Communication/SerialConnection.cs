using System;
using System.IO.Ports;

namespace MaxMix.Services.Communication
{
    // Keep the serial boundary replaceable so disconnects and delayed replies can
    // be exercised without opening a real device.
    internal interface ISerialConnection : IDisposable
    {
        bool IsOpen { get; }
        int BytesToRead { get; }
        int ReadTimeout { get; set; }
        int WriteTimeout { get; set; }
        void Open();
        void Close();
        void DiscardInBuffer();
        void DiscardOutBuffer();
        int ReadByte();
        string ReadLine();
        int Read(byte[] buffer, int offset, int count);
        void Write(byte[] buffer, int offset, int count);
    }

    internal sealed class SerialConnection : SerialPort, ISerialConnection
    {
        public SerialConnection(string portName) : base(portName, 76800) { }
    }
}
