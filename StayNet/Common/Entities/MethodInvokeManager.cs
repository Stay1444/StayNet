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



        public async Task<byte[]> SendInvoke(CancellationToken token, bool wait)
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

            if (wait)
            {
                var responseTask = PacketHandler.WaitForPacket(BasePacketTypes.Message,x =>
                    x.Packet.ReadByte(true) == (byte) MethodInvokeManagerPacketType.InvokeAck 
                    && x.Packet.ReadInt(true) == responseId);
                await PacketSender.SendAsync(packet, BasePacketTypes.Message);
                var response = await responseTask;
                if (response is null)
                {
                    return null;
                }

                response.Packet.ReadByte();
                response.Packet.ReadInt();
                int responseLength = response.Packet.ReadInt();
                if (responseLength == 0)
                {
                    return null;
                }
            }
            else
            {
                await PacketSender.SendAsync(packet, BasePacketTypes.Message);

            }

            //byte[] result = response.Packet.ReadBytes(response.Packet.ReadInt(true));
            return new byte[]{3};   
        }
        
    }
}