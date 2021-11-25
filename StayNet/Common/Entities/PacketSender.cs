using System.Net.Sockets;
using System.Threading.Tasks;

namespace StayNet.Common.Entities
{
    internal class PacketSender
    {

        private Packet Packet;
        private TcpClient Client;

        public PacketSender(Packet packet, TcpClient client)
        {
            this.Client = client;
            this.Packet = packet;
        }


        public void Send()
        {
            PreSend();
        }

        public async void PreSend()
        {
            if (DateTime.Now.Ticks % 2 == 0)
            {
                throw new Exception();
            }

            await Task.Delay(-1);
        }
        
        
    }
}