using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using StayNet.Client.Entities;
using StayNet.Common.Controllers;
using StayNet.Common.Entities;
using StayNet.Common.Enums;
using StayNet.Common.Interfaces;

namespace StayNet
{

    public class StayNetClientConfiguration
    {
        public ILogger Logger { get; set; }
    }
    
    public sealed class StayNetClient
    {
        
        public IPEndPoint EndPoint { get; private set; }
        public StayNetClientConfiguration Configuration { get; private set; }
        public bool IsConnected { get; private set; }
        internal TcpClient TcpClient { get; private set; }
        internal List<byte> _receiveBuffer = new List<byte>();
        internal PacketHandler PacketHandler;
        internal ControllerManager ControllerManager;
        public StayNetClient(IPEndPoint endpoint, StayNetClientConfiguration config)
        {
            this.EndPoint = endpoint;
            this.Configuration = config;
            PacketHandler = new PacketHandler(this);
            ControllerManager = new ControllerManager();
            PacketHandler.PacketReceived += PacketHandlerOnPacketReceived;
        }

        private void PacketHandlerOnPacketReceived(object? sender, PacketInfo e)
        {

            if (e.PacketType == BasePacketTypes.Message)
            {
                MethodInvokeManagerPacketType type = (MethodInvokeManagerPacketType) e.Packet.ReadByte();

                if (type == MethodInvokeManagerPacketType.PreInvoke)
                {

                    string responseId = e.Packet.ReadString();
                    string methodName = e.Packet.ReadString();
                    Packet responsePacket = new Packet();
                    responsePacket.WriteByte((byte)MethodInvokeManagerPacketType.PreInvokeAck);
                    responsePacket.WriteString(responseId);
                    responsePacket.WriteBool(ControllerManager.IsValidMethod(methodName));
                    PacketSender psender = new PacketSender(this.TcpClient, responsePacket, BasePacketTypes.Message);
                    psender.SendAsync().GetAwaiter().GetResult();
                }
                
            }else if (e.PacketType == BasePacketTypes.KeepAlive)
            {
                Packet responsePacket = new Packet();
                responsePacket.WriteByte((byte)BasePacketTypes.KeepAlive);
                PacketSender psender = new PacketSender(this.TcpClient, responsePacket, BasePacketTypes.KeepAlive);
                psender.SendAsync().GetAwaiter().GetResult();
            }
            
        }

        internal void Log(LogLevel level, string message)
        {
            Configuration.Logger?.Log(message, level,this);
        }
        
        public void RegisterController<T>() where T : BaseController
        {
            ControllerManager.RegisterController<T>();
        }

        public async Task ConnectAsync(String data)
        {
            await RawConnectAsync(System.Text.Encoding.UTF8.GetBytes(data));
        }

        public async Task ConnectAsync()
        {
            await RawConnectAsync(Array.Empty<byte>());
        }


        private byte[] _buffer;
        private async Task RawConnectAsync(byte[] data)
        {
            try
            {
                Log(LogLevel.Info, "Connecting");
                TcpClient = new TcpClient();
                TcpClient.ReceiveBufferSize = 8192;
                TcpClient.SendBufferSize = 8192;
                await TcpClient.ConnectAsync(EndPoint);
                IsConnected = true;
                _buffer = new byte[TcpClient.ReceiveBufferSize];
                TcpClient.GetStream().BeginRead(_buffer, 0, TcpClient.ReceiveBufferSize, __read, null);
                await SendInitialMessage(data);
            }
            catch (TimeoutException ex)
            {
                throw ex;
            }
            catch(Exception e)
            {
                Log(LogLevel.Error, "Connection failed, disconnecting.");
                Log(LogLevel.Error, e.Message);
                Close();
            }
            
        }

        async Task SendInitialMessage(byte[] data)
        {
            Packet packet = new Packet();
            packet.WriteBytes(data);
            PacketSender sender = new PacketSender(this.TcpClient, packet, BasePacketTypes.InitialMessage);
            await sender.SendAsync();            
        }


        void __read(IAsyncResult asyncResult)
        {
            try
            {
                int readLength = TcpClient.GetStream().EndRead(asyncResult);
                if (readLength == 0)
                {
                    Log(LogLevel.Info, $"Server disconnected");
                    Disconnect();
                    return;
                }
                byte[] data = new byte[readLength];
                
                Array.Copy(_buffer, data, readLength);
                
                _receiveBuffer.AddRange(data);
                HandleData();
                _buffer = new byte[TcpClient.ReceiveBufferSize];
                TcpClient.GetStream().BeginRead(_buffer, 0, TcpClient.ReceiveBufferSize, __read, null);
            }catch(Exception e)
            {
                Log(LogLevel.Info, $"Server disconnected {e.Message}");
                this.Disconnect();
            }
        }

        private void HandleData()
        {

            var data = _receiveBuffer.ToArray();
            if (data.Length < 4)
                return;
            
            int length = BitConverter.ToInt32(data, 0);
            
            byte[] packet = new byte[length];
            Array.Copy(data, 4, packet, 0, length);
            _receiveBuffer.RemoveRange(0, length + 4);
            
            PacketHandler.Handle(packet);            

        }

        internal void Close()
        {
            TcpClient.Close();
            IsConnected = false;
        }

        public void Disconnect()
        {
            Close();
            Log(LogLevel.Info, "Disconnected");
        }
        
        public async Task InvokeAsync(String Message, params object[] args)
        {
            CancellationTokenSource cts = new CancellationTokenSource();
            cts.CancelAfter(5000);
            cts.Token.ThrowIfCancellationRequested();
            await Task.Delay(5001);
            Console.WriteLine("hi!");
        }
        
    }
}