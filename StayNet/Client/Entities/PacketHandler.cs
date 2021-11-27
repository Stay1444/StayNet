using System.Collections.Generic;
using System.Linq.Expressions;
using System.Threading.Tasks;
using StayNet.Common.Entities;
using StayNet.Common.Enums;

namespace StayNet.Client.Entities
{
    internal class PacketHandler
    {
        
        public List<PacketInfo> Packets { get; set; }
        private StayNetClient _client;
        public event EventHandler<PacketInfo> PacketReceived;
        public PacketHandler(StayNetClient client)
        {
            this._client = client;
            Packets = new List<PacketInfo>();
        }

        public void Handle(byte[] data)
        {
            BasePacketTypes packetType = (BasePacketTypes)data[0];
            byte[] packetData = new byte[data.Length - 1];
            Array.Copy(data, 1, packetData, 0, data.Length - 1);
            Packet packet = new Packet(packetData);
            PacketInfo packetInfo = new PacketInfo(packet, packetType);
            Packets.Add(packetInfo);
            PacketReceived?.Invoke(_client, packetInfo);
        }
        
    }
}