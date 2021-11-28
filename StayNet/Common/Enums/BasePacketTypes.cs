namespace StayNet.Common.Enums
{
    public enum BasePacketTypes : byte
    {
        InitialMessage = 0,
        InitialMessageAck = 1,
        KeepAlive = 2,
        Message = 3,
        FileData = 4,
    }
}