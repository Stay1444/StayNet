namespace StayNet.Common.Enums
{
    public enum BasePacketTypes : byte
    {
        InitialMessage = 0,
        KeepAlive = 1,
        Message = 2,
        FileData = 3,
    }
}