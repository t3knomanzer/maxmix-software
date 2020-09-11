using System;
using MaxMix.Services.Communication.Messages;

namespace MaxMix.Services.Communication.Serialization
{
    internal interface ISerializationService
    {
        byte Delimiter { get; }

        void RegisterType<T>(int id) where T : IMessage;
        byte[] Serialize(IMessage message, byte revision);
        IMessage Deserialize(byte[] bytes);
    }
}