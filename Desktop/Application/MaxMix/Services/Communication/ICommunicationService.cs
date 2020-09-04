using System;

namespace MaxMix.Services.Communication
{
    internal interface ICommunicationService
    {
        /// <summary>
        /// 
        /// </summary>
        event EventHandler<IMessage> MessageReceived;

        /// <summary>
        /// 
        /// </summary>
        event EventHandler<string> Error;

        /// <summary>
        /// 
        /// </summary>
        event EventHandler<string> DeviceDiscovered;

        /// <summary>
        /// 
        /// </summary>
        void Start();

        /// <summary>
        /// 
        /// </summary>
        void Stop();

        /// <summary>
        /// 
        /// </summary>
        /// <param name="message"></param>
        /// <returns></returns>
        bool Send(IMessage message);
    }
}