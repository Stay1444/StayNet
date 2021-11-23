using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
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
        public StayNetClient(IPEndPoint endpoint, StayNetClientConfiguration config)
        {
            this.EndPoint = endpoint;
            this.Configuration = config;
        }

        internal void Log(LogLevel level, string message)
        {
            Configuration.Logger?.Log(message, level,this);
        }

        public async Task ConnectAsync(String data)
        {
            await RawConnectAsync(System.Text.Encoding.UTF8.GetBytes(data));
        }

        public async Task ConnectAsync()
        {
            await RawConnectAsync(Array.Empty<byte>());
        }

        public void Connect()
        {
            Task.Run(() => RawConnectAsync(Array.Empty<byte>()));
        }
        
        public void Connect(String data)
        {
            Task.Run(() => RawConnectAsync(System.Text.Encoding.UTF8.GetBytes(data)));
        }

        private async Task RawConnectAsync(byte[] data)
        {
            try
            {
                Log(LogLevel.Info, "Connecting");
                TcpClient = new TcpClient();
                TcpClient.ReceiveBufferSize = 8192;
                TcpClient.SendBufferSize = 8192;
                await TcpClient.ConnectAsync(EndPoint);
                byte[] addedData = new byte[data.Length + 1];
                addedData[0] = (byte) BasePacketTypes.InitialMessage;
                Array.Copy(data, 0, addedData, 1, data.Length);
                await TcpClient.GetStream().WriteAsync(addedData, 0, addedData.Length);
                IsConnected = true;
                //wait for read
                try
                {
                    CancellationTokenSource cts = new CancellationTokenSource();
                    cts.CancelAfter(5000);
                    while (!TcpClient.GetStream().DataAvailable && !cts.IsCancellationRequested)
                    {
                        await Task.Delay(50);
                    }
        
                    if (cts.IsCancellationRequested)
                    {
                        Close();
                        Log(LogLevel.Error, "Connection timed out");
                        throw new TimeoutException("Connection timed out");
                    }

                    byte[] buffer = new byte[TcpClient.Available];
                    await TcpClient.GetStream().ReadAsync(buffer, 0, buffer.Length);

                    if (buffer[0] == 1)
                    {
                        Log(LogLevel.Info, $"Connection successful");
                        while (true)
                        {
    
                        }
                    }
                    else
                    {
                        Log(LogLevel.Error, "Connection failed, disconnecting.");
                        Close();
                        return;
                    }

                }
                catch (TaskCanceledException)
                {
                    Log(LogLevel.Error, "Connection timed out, disconnecting.");
                    Close();
                    return;
                }
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