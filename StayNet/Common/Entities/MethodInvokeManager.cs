using System.Collections.Generic;
using System.Linq;
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
        private PacketHandler PacketHandler;
        public static MethodInvokeManager Create(TcpClient client, PacketHandler handler, CancellationToken ct, string messageId, object[] invArgs, MethodInvokeManagerReturnType returnType)
        {
            
            
            MethodInvokeManager manager = new MethodInvokeManager();
            manager.CToken = ct;
            manager.PacketHandler = handler;
            manager._client = client;
            manager.MethodName = messageId;
            manager.Parameters = invArgs;
            manager.expectedReturnType = returnType;
            return manager;
        }


        public async Task<bool> SendPreInvoke()
        {
            CancellationTokenSource cts = new CancellationTokenSource();
            cts.CancelAfter(TimeSpan.FromSeconds(5));
            Packet packet = new Packet();
            packet.WriteByte((byte) MethodInvokeManagerPacketType.PreInvoke);
            string responseId = Guid.NewGuid().ToString();
            packet.WriteString(responseId);
            packet.WriteString(MethodName);
            PacketSender sender = new PacketSender(_client, packet, BasePacketTypes.Message);
            var responseTask = PacketHandler.WaitForPacket(x => x.PacketType == BasePacketTypes.Message
            && x.Packet.ReadByte(true) == (byte) MethodInvokeManagerPacketType.PreInvokeAck && x.Packet.ReadString(true) == responseId, cts.Token);
            
            
            await sender.SendAsync();
            var response = await responseTask;
            if (response == null)
            {
                throw new TimeoutException("PreInvoke timed out");
            }
            response.Packet.ReadByte();
            response.Packet.ReadString();
            bool isSuccess = response.Packet.ReadBool();

            return isSuccess;
        }
    }
}