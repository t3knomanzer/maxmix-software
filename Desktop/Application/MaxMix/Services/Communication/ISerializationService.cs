using System;

namespace MaxMix.Services.Communication
{
    internal interface ISerializationService
    {
        byte Delimiter { get; }

        void RegisterType<T>(int id) where T : IMessage;
        byte[] Serialize(IMessage message);
        IMessage Deserialize(byte[] bytes);
    }
}