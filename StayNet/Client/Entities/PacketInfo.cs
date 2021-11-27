using StayNet.Common.Entities;
using StayNet.Common.Enums;

namespace StayNet.Client.Entities
{
    internal class PacketInfo
    {

        public Packet Packet;
        public BasePacketTypes PacketType { get; private set; }
        
        public PacketInfo(Packet packet, BasePacketTypes type)
        {
            Packet = packet;
            PacketType = type;
        }
        
    }
}