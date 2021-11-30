using System.Collections.Generic;
using System.Diagnostics;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using StayNet.Common.Enums;

namespace StayNet.Common.Entities
{
    internal class PacketSender
    {
        private static int _responseIdUsed;
        private TcpClient _client;
        private NetworkStream _stream;

        public PacketSender(TcpClient client)
        {
            _client = client;
            _stream = client.GetStream();
        }
        
        public static int GetNextResponseId()
        {
            if (_responseIdUsed == int.MaxValue)
            {
                _responseIdUsed = 0;
            }
            else
            {
                _responseIdUsed++;
            }

            return _responseIdUsed;
        }

        public async Task SendAsync(Packet packet, BasePacketTypes basePacketTypes)
        {
            byte[] _packet = new byte[packet.Length + 1];
            _packet[0] = (byte)basePacketTypes;
            Array.Copy(packet.Data, 0, _packet, 1, packet.Length);
            
            var data = new byte[_packet.Length + 4];
            var length = BitConverter.GetBytes(_packet.Length);
            Array.Copy(length, data, 4);
            Array.Copy(_packet, 0, data, 4, _packet.Length);
            //await _stream.WriteAsync(data, 0, data.Length);
            await _client.Client.SendAsync(data, SocketFlags.None);
        }
        
    }
}