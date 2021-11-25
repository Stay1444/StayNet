using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using StayNet.Common.Enums;
using StayNet.Server;

namespace StayNet.Common.Entities
{
    internal class MethodInvokeManager
    {
        
        private MethodInvokeManager(){}
        private TcpClient _client;
        private CancellationToken CToken;
        private String MethodName;
        private Object[] Parameters;
        private MethodInvokeManagerReturnType expectedReturnType;
        private Object ReturnValue;
        public static MethodInvokeManager Create(TcpClient client, CancellationToken ct, string messageId, object[] invArgs, MethodInvokeManagerReturnType returnType)
        {
            
            
            MethodInvokeManager manager = new MethodInvokeManager();
            manager.CToken = ct;
            manager._client = client;
            manager.MethodName = messageId;
            manager.Parameters = invArgs;
            manager.expectedReturnType = returnType;
            return manager;
        }


        public async Task SendPreInvoke()
        {
            Packet packet = new Packet();
            packet.WriteByte((byte)BasePacketTypes.Message);
            packet.WriteByte(0);
            packet.WriteString(MethodName);
            
            Console.WriteLine($"PreInvoke weights {packet.Data.Length}");
            
        }
    }
}