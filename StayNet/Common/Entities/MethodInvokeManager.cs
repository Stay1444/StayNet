using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using StayNet.Client.Entities;
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
        private PacketSender PacketSender;
        public static MethodInvokeManager Create(TcpClient client, PacketSender PacketSender, PacketHandler handler, CancellationToken ct, string messageId, object[] invArgs, MethodInvokeManagerReturnType returnType)
        {
            
            MethodInvokeManager manager = new MethodInvokeManager();
            manager.CToken = ct;
            manager.PacketHandler = handler;
            manager.PacketSender = PacketSender;
            manager._client = client;
            manager.MethodName = messageId;
            manager.Parameters = invArgs;
            manager.expectedReturnType = returnType;
            return manager;
        }


        public async Task<bool> SendPreInvoke()
        {
            return true;
            CancellationTokenSource cts = new CancellationTokenSource();
            cts.CancelAfter(TimeSpan.FromSeconds(5));
            Packet packet = Packet.Create();
            packet.WriteByte((byte) MethodInvokeManagerPacketType.PreInvoke);
            int responseId = PacketSender.GetNextResponseId();
            packet.WriteInt(responseId);
            packet.WriteString(MethodName);
            var responseTask = PacketHandler.WaitForPacket(x => x.PacketType == BasePacketTypes.Message
            && x.Packet.ReadByte(true) == (byte) MethodInvokeManagerPacketType.PreInvokeAck && x.Packet.ReadInt(true) == responseId, cts.Token);
            
            
            await PacketSender.SendAsync(packet, BasePacketTypes.Message);
            var response = await responseTask;
            if (response == null)
            {
                throw new TimeoutException("PreInvoke timed out");
            }
            response.Packet.ReadByte();
            response.Packet.ReadInt();
            bool isSuccess = response.Packet.ReadBool();

            return isSuccess;
        }

        public async Task<byte[]> SendInvoke(CancellationToken token)
        {
            token.ThrowIfCancellationRequested();
            Packet packet = Packet.Create();
            packet.WriteByte((byte) MethodInvokeManagerPacketType.Invoke);
            int responseId = PacketSender.GetNextResponseId();
            packet.WriteInt(responseId);
            packet.WriteString(MethodName);
            packet.WriteInt(Parameters.Length);
            foreach (var parameter in Parameters)
            {
                if (parameter.GetType().FullName == null)
                    continue;
                
                packet.WriteString(parameter.GetType().FullName);
                
                var type = parameter.GetType();

                if (type == typeof(int))
                {
                    packet.WriteInt((int)parameter);
                }else if (type == typeof(string))
                {
                    packet.WriteString((string)parameter);
                }else if (type == typeof(bool))
                {
                    packet.WriteBool((bool)parameter);
                }else if (type == typeof(byte))
                {
                    packet.WriteByte((byte)parameter);
                }else if (type == typeof(short))
                {
                    packet.WriteShort((short)parameter);
                }else if (type == typeof(long))
                {
                    packet.WriteLong((long)parameter);
                }else if (type == typeof(float))
                {
                    packet.WriteFloat((float)parameter);
                }else if (type == typeof(double))
                {
                    packet.WriteDouble((double) parameter);
                }
                else
                {
                    packet.WriteString(JsonConvert.SerializeObject(parameter));
                }


            }
            
            //var responseTask = PacketHandler.WaitForPacket(x => x.PacketType == BasePacketTypes.Message
             //   && x.Packet.ReadByte(true) == (byte) MethodInvokeManagerPacketType.InvokeAck 
              //  && x.Packet.ReadInt(true) == responseId, token);
            await PacketSender.SendAsync(packet, BasePacketTypes.Message);
            //var response = await responseTask;
            PacketInfo response = null;
            if (response == null)
            {
                return new []{(byte)0};
                //throw new TimeoutException("Invoke timed out");
            }
            response.Packet.ReadByte();
            response.Packet.ReadInt();
            
            int responseLength = response.Packet.ReadInt();
            if (responseLength == 0)
            {
                return null;
            }

            //byte[] result = response.Packet.ReadBytes(response.Packet.ReadInt(true));
            return new byte[]{3};   
        }
        
    }
}