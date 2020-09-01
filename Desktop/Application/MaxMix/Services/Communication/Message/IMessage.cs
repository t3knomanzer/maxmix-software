namespace MaxMix.Services.Communication
{
    internal interface IMessage
    {
        byte[] GetBytes();
        bool SetBytes(byte[] bytes);
        byte GetRevision();
        bool SetRevision(byte revision);
    }
}