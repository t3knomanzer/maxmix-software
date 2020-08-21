using System;

namespace MaxMix.Services.Communication
{
    internal interface IDiscoveryService
    {
        event EventHandler<string> DeviceDiscovered;
        event EventHandler<string> Error;

        void Start(int baudRate);
        void Stop();
    }
}