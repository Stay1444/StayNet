using System.Net.Sockets;
using System.Threading.Tasks;
using StayNet.Common.Enums;

namespace StayNet.Common.Entities
{
    internal class PacketSender
    {
        private readonly TcpClient _client;
        private readonly NetworkStream _stream;
        private readonly byte[] _packet;
        
        public PacketSender(TcpClient client, Packet packet, BasePacketTypes basePacketTypes)
        {
            _client = client;
            _stream = client.GetStream();
            _packet = new byte[packet.Length + 1];
            _packet[0] = (byte)basePacketTypes;
            Array.Copy(packet.Data, 0, _packet, 1, packet.Length);
        }
        
        public async Task SendAsync()
        {
            var stream = _client.GetStream();
            var data = new byte[_packet.Length + 4];
            var length = BitConverter.GetBytes(_packet.Length);
            Array.Copy(length, data, 4);
            Array.Copy(_packet, 0, data, 4, _packet.Length);

            await stream.WriteAsync(data, 0, data.Length);
        }
        
    }
}