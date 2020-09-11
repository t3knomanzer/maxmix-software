namespace MaxMix.Services.Communication.Messages
{
    internal interface IMessage
    {
        byte[] GetBytes();
        bool SetBytes(byte[] bytes);
    }
}