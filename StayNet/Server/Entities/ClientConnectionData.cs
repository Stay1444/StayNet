using System.IO;
using System.Text;
using StayNet.Common.Entities;

namespace StayNet.Server.Entities
{
    public sealed class ClientConnectionData
    {

        internal Packet Packet { get; set; }


        public string ReadString()
        {
            return Encoding.UTF8.GetString(Packet.Data);
        }
        
        public byte[] ReadBytes()
        {
            return Packet.Data;
        }
        
    }
}