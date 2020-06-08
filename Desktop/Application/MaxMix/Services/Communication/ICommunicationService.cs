using System;

namespace MaxMix.Services.Communication
{
    internal interface ICommunicationService
    {
        event EventHandler<IMessage> MessageReceived;
        event EventHandler<string> Error;

        void Start(string portName, int baudRate);
        void Stop();
        void Send(IMessage payload);
    }
}