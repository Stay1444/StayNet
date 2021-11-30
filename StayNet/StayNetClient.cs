using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Timers;
using Newtonsoft.Json;
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
        internal PacketSender PacketSender;
        internal System.Timers.Timer HeartbeatTimer;
        public StayNetClient(IPEndPoint endpoint, StayNetClientConfiguration config)
        {
            this.EndPoint = endpoint;
            this.Configuration = config;
            PacketHandler = new PacketHandler(this);
            ControllerManager = new ControllerManager();
            PacketHandler.PacketReceived += (sender, info) =>
            {
                Task.Run(() => PacketHandlerOnPacketReceived(sender, info));
            };
        }

        private async Task PacketHandlerOnPacketReceived(object? sender, PacketInfo e)
        {

            if (e.PacketType == BasePacketTypes.Message)
            {
                MethodInvokeManagerPacketType type = (MethodInvokeManagerPacketType) e.Packet.ReadByte();

                if (type == MethodInvokeManagerPacketType.PreInvoke)
                {

                    int responseId = e.Packet.ReadInt();
                    string methodName = e.Packet.ReadString();
                    Packet responsePacket = Packet.Create();
                    responsePacket.WriteByte((byte)MethodInvokeManagerPacketType.PreInvokeAck);
                    responsePacket.WriteInt(responseId);
                    responsePacket.WriteBool(ControllerManager.IsValidMethod(methodName));
                    PacketSender.SendAsync(responsePacket, BasePacketTypes.Message).GetAwaiter().GetResult();
                    return;
                }

                if (type == MethodInvokeManagerPacketType.Invoke)
                {
                    int responseId = e.Packet.ReadInt();
                    string methodName = e.Packet.ReadString();
                    int argCount = e.Packet.ReadInt();
                    object[] args = new object[argCount];
                    for (int i = 0; i < argCount; i++)
                    {
                        string argType = e.Packet.ReadString();
                        Type t = Type.GetType(argType);
                        if (t == null)
                            continue;

                        if (t == typeof(string))
                        {
                            args[i] = e.Packet.ReadString();
                        }else if (t == typeof(int))
                        {
                            args[i] = e.Packet.ReadInt();
                        }else if (t == typeof(bool))
                        {
                            args[i] = e.Packet.ReadBool();
                        }else if (t == typeof(float))
                        {
                            args[i] = e.Packet.ReadFloat();
                        }else if (t == typeof(double))
                        {
                            args[i] = e.Packet.ReadDouble();
                        }else if (t == typeof(byte))
                        {
                            args[i] = e.Packet.ReadByte();
                        }else if (t == typeof(short))
                        {
                            args[i] = e.Packet.ReadShort();
                        }else if (t == typeof(long))
                        {
                            args[i] = e.Packet.ReadLong();
                        }
                        else
                        {
                            args[i] = JsonConvert.DeserializeObject(e.Packet.ReadString());
                        }

                    }
                    Packet responsePacket = Packet.Create();
                    responsePacket.WriteByte((byte)MethodInvokeManagerPacketType.InvokeAck);
                    responsePacket.WriteInt(responseId);
                    
                    if (ControllerManager.CanInvokeMethod(methodName, args))
                    {
                        await ControllerManager.InvokeMethod(methodName, args);
                        responsePacket.WriteInt(0);
                    }
                    else
                    {
                        responsePacket.WriteInt(0);
                    }
                    await PacketSender.SendAsync(responsePacket, BasePacketTypes.Message);
                }
                
            }else if (e.PacketType == BasePacketTypes.KeepAlive)
            {
                Packet responsePacket = Packet.Create();
                responsePacket.WriteByte((byte)BasePacketTypes.KeepAlive);
                PacketSender.SendAsync(responsePacket, BasePacketTypes.KeepAlive).GetAwaiter().GetResult();
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
                this.PacketSender = new PacketSender(this.TcpClient);
                this.HeartbeatTimer = new System.Timers.Timer(150);
                this.HeartbeatTimer.Elapsed += HeartbeatTimer_Elapsed;
                this.HeartbeatTimer.Start();
                this.HeartbeatTimer.AutoReset = true;
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

        private void HeartbeatTimer_Elapsed(object? sender, ElapsedEventArgs e)
        {
            if (IsConnected)
            {
                //HandleData();
            }
        }

        async Task SendInitialMessage(byte[] data)
        {
            Packet packet = Packet.Create();
            packet.WriteBytes(data);
            await PacketSender.SendAsync(packet, BasePacketTypes.InitialMessage);            
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
            if (length > data.Length)
                return;
            byte[] packet = new byte[length];
            Array.Copy(data, 4, packet, 0, length);
            _receiveBuffer.RemoveRange(0, length + 4);
            
            PacketHandler.Handle(packet);            
            HandleData();
            _receiveBuffer.Print();
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