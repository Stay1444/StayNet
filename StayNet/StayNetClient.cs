using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using StayNet.Client.Entities;
using StayNet.Common.Controllers;
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
                byte type = e.Packet.ReadByte();

                if (type == 0)
                {
                    Log(LogLevel.Debug, "Received Message");
                }
                
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
            byte[] message = new byte[data.Length + 1];
            message[0] = (byte)BasePacketTypes.InitialMessage;
            await TcpClient.GetStream().WriteAsync(message, 0, message.Length);
        }


        void __read(IAsyncResult asyncResult)
        {
            try
            {
                int readLength = TcpClient.GetStream().EndRead(asyncResult);
                byte[] data = new byte[ readLength];
                _buffer.CopyTo(data, 0);
                if (readLength == 0)
                {
                    Log(LogLevel.Info, $"Server disconnected");
                    Disconnect();
                    return;
                }
                _receiveBuffer.AddRange(data);
                HandleData();
                _buffer = new byte[TcpClient.ReceiveBufferSize];
                TcpClient.GetStream().BeginRead(_buffer, 0, TcpClient.ReceiveBufferSize, __read, null);
            }catch(Exception e)
            {
                Log(LogLevel.Info, $"Server disconnected");
                this.Disconnect();
            }
        }

        private void HandleData()
        {

            var data = _receiveBuffer.ToArray();
            int length = BitConverter.ToInt32(data, 0);

            if (length < 4) return;
            
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