using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Runtime.Serialization;
using System.Threading;
using System.Threading.Tasks;
using System.Timers;
using Newtonsoft.Json;
using StayNet.Common.Entities;
using StayNet.Common.Enums;
using StayNet.Common.Exceptions;
using StayNet.Server.Entities;
using StayNet.Server.Events;

namespace StayNet.Server
{
    public class Client
    {

        public int Id { get; internal set; }
        
        internal TcpClient TcpClient;
        public StayNetServer Server { get; internal set; }
        internal byte[] Buffer;
        internal CancellationTokenSource CancellationTokenSource;
        internal PacketHandler PacketHandler;
        internal PacketSender PacketSender;
        internal List<byte> _receiveBuffer = new List<byte>();
        private System.Timers.Timer _keepAliveTimer;
        public int Ping { get; private set; }
        internal Client(TcpClient tcpclient, StayNetServer server)
        {
            this.TcpClient = tcpclient;
            this.Server = server;
            this.Id = this.TcpClient.Client.Handle.ToInt32();
            while (server.m_clients.ContainsKey(this.Id))
            {
                this.Id++;
            }
            PacketHandler = new PacketHandler(this);
            _keepAliveTimer = new(1000);
            _keepAliveTimer.Elapsed += KeepAliveTimerOnElapsed;
            _keepAliveTimer.AutoReset = false;
            PacketSender = new PacketSender(this.TcpClient);
        }

        private void KeepAliveTimerOnElapsed(object? sender, ElapsedEventArgs e)
        {
            return;
            Packet packet = Packet.Create();
            packet.WriteByte(1);
            CancellationTokenSource cts = new CancellationTokenSource();
            cts.CancelAfter(5000);
            var responseTask = PacketHandler.WaitForPacket(BasePacketTypes.KeepAlive,p => p.PacketType == BasePacketTypes.KeepAlive, cts.Token);
            Stopwatch sw = new Stopwatch();
            sw.Start();
            PacketSender.SendAsync(packet, BasePacketTypes.KeepAlive).GetAwaiter().GetResult();
            var response = responseTask.GetAwaiter().GetResult();
            sw.Stop();
            if (response == null)
            {
                this.Server.Log(LogLevel.Info,$"Client {this.Id} timed out");
                this.Disconnect();
            }
            else
            {
                Ping = (int)sw.ElapsedMilliseconds;
                _keepAliveTimer.Start();
            }
            
            
        }

        /**
         * Wait for the client to send Initial Connection packet. Usually containing things like authorization, etc.
         */
        internal async Task<ClientConnectionData> WaitForConnectionData()
        {
            //we need to wait for the client to send us the initial connection data
            Buffer = new byte[TcpClient.ReceiveBufferSize];
            CancellationTokenSource cts = new CancellationTokenSource();
            try
            {
                cts.CancelAfter(5000);
                var packetInfoTask = PacketHandler.WaitForPacket(BasePacketTypes.InitialMessage,_ => true, cts.Token);
                TcpClient.GetStream().BeginRead(Buffer, 0, TcpClient.ReceiveBufferSize, __read, null);
                var packetInfo = await packetInfoTask;
                if (cts.IsCancellationRequested)
                {

                    this.Server.Log(LogLevel.Debug, $"Client {this.Id} timed out waiting for connection data");

                    return null;
                }

                var connectionData = new ClientConnectionData();
                // if the id doesnt match the InitialMessage packet id, we need to disconnect the client. In this case, we just return null 
                // and the client will be disconnected.
                if (packetInfo.PacketType != BasePacketTypes.InitialMessage)
                {
                    this.Server.Log(LogLevel.Debug, $"Client {this.Id} sent invalid id {packetInfo.PacketType.ToString("X")}");
                    return null;
                }
                // convert the list to a byte array and set the connection data
                connectionData.Packet = packetInfo.Packet;
                return connectionData;
            }catch(Exception e)
            {
                this.Server.Log(LogLevel.Debug, $"Client {this.Id} disconnected: {e.Message}");
                return null;
            }
        }
        
        internal async Task EndInitialization()
        {
            CancellationTokenSource = new CancellationTokenSource();
            _receiveBuffer = new List<byte>();
            _keepAliveTimer.Start();

        }


        internal void __read(IAsyncResult result)
        {
            
            try
            {
                int readLength = TcpClient.GetStream().EndRead(result);
                if (readLength == 0)
                {
                    this.Server.Log(LogLevel.Info, $"Client {this.Id} disconnected");
                    this.Disconnect();
                    return;
                }

                byte[] data = new byte[readLength];
                Array.Copy(Buffer, data, readLength);

                _receiveBuffer.AddRange(data);
                
                HandleData();
                Buffer = new byte[TcpClient.ReceiveBufferSize];
                TcpClient.GetStream().BeginRead(Buffer, 0, TcpClient.ReceiveBufferSize, __read, null);

            }catch(Exception e)
            {
                if (TcpClient.Connected == false)
                    return;
                this.Server.Log(LogLevel.Info, $"Client {this.Id} disconnected. {e.Message}");
                this.Disconnect();
            }

        }

        private void HandleData()
        {
            try
            {
                var data = _receiveBuffer.ToArray();
                if (data.Length < 4)
                    return;
                int length = BitConverter.ToInt32(data, 0);
                
                if (length > data.Length)
                    return;
                
                byte[] packet = new byte[length];
                
                if (data.Length - 4 < length)
                    return;
                
                Array.Copy(data, 4, packet, 0, length);
                _receiveBuffer.RemoveRange(0, length + 4);
                try
                {
                    PacketHandler.Handle(packet);

                }catch(Exception e)
                {
                    this.Server.Log(LogLevel.Error, "Error handling packet: " + e.Message);
                }
                HandleData();

            }catch(Exception e)
            {
                this.Server.Log(LogLevel.Error, "Error handling packet: " + e.Message);
            }
        }

        internal void Close()
        {
            TcpClient.Close();
        }
        
        public void Disconnect()
        {
            CancellationTokenSource?.Cancel();
            this.Server.m_clients.Remove(this.Id);
            this.Server.CDisconnect(this);
            _keepAliveTimer.Stop();
            this.Close();
        }
        
        public async Task InvokeAsync(String MethodId, params object[] args)
        {


            MethodInvokeManager methodInvokeManager = MethodInvokeManager.Create(this.TcpClient, this.PacketSender, this.PacketHandler, CancellationTokenSource.Token, 
                MethodId, args, MethodInvokeManagerReturnType.None);

            try
            {

                CancellationTokenSource cts = new CancellationTokenSource();
                cts.CancelAfter(5000);
                await methodInvokeManager.SendInvoke(cts.Token,false);
            }
            catch (TimeoutException e)
            {
                this.Server.Log(LogLevel.Debug, $"Error sending message {MethodId} to client {this.Id}: {e.Message}");
            }
            catch (MethodNotFoundException e)
            {
                throw;
                
            }catch(Exception e)
            {
                this.Server.Log(LogLevel.Debug, $"Error sending message {MethodId} to client {this.Id}: {e.Message}");
                Console.WriteLine(e);
                Console.WriteLine(e);
            }
        }

        public async Task<T> InvokeAsync<T>(String MethodId, params object[] args)
        {
            return default(T);
        }
        
        
        
    }
}