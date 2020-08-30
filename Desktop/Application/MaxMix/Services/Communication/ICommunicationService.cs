using System;

namespace MaxMix.Services.Communication
{
    internal interface ICommunicationService
    {
        event EventHandler<IMessage> MessageReceived;
        event EventHandler<string> Error;
        event EventHandler<string> DeviceDiscovered;

        void Start();
        void Stop();
        bool Send(IMessage message);
    }
}