using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using StayNet.Common.Enums;
using StayNet.Common.Interfaces;
using StayNet.Common.Controllers;
using StayNet.Server.Entities;
using StayNet.Server.Events;
using StayNet.Server.Exceptions;
using StayNet.Server;
namespace StayNet
{
    public sealed class StayNetServerConfiguration
    {
        public IPAddress Host { get; set; } 
        public int Port { get; set; }
        public IServiceProvider ServiceProvider { get; set; }
        public ILogger Logger { get; set; }
        public LogLevel LogLevel = LogLevel.Info;
        public int MaxConnections = 0;
    }
    public sealed class StayNetServer : IDisposable
    {

        #region Events
        
        public event EventHandler<Server.Client> ClientConnected;
        public event EventHandler<Server.Client> ClientDisconnected;
        public event EventHandler<ClientConnectingEvent> ClientConnecting;

        #endregion

        #region Public

        public readonly StayNetServerConfiguration Configuration;
        public bool IsRunning { get; private set; }
        
        #endregion

        #region Internal

        internal ControllerManager m_controllerManager;
        
        internal TcpListener m_listener;

        internal CancellationTokenSource m_cancellation;

        internal Dictionary<int, Server.Client> m_clients = new();
        
        internal void Log(LogLevel level, string message)
        {
            if (Configuration.LogLevel <= level)
            {
                Configuration.Logger?.Log(message, level, this);
            }
        }
        
        #endregion

        public StayNetServer(StayNetServerConfiguration configuration)
        {
            this.Configuration = configuration;
            m_controllerManager = new ControllerManager();
        }

        #region Methods

        public void Start()
        {
            if (IsRunning)
            {
                throw new ServerStateException("The server is already running!", this);
            }
            m_cancellation = new CancellationTokenSource();
            m_listener = new TcpListener(Configuration.Host, Configuration.Port);
            m_listener.Start();
            Task.Run(AcceptClientsAsync);
            Log(LogLevel.Info, "Server Started Successfully");
        }

        private async Task AcceptClientsAsync()
        {
            Log(LogLevel.Debug, $"Listening task started. [{Thread.CurrentThread.ManagedThreadId}]");
            
            while (!m_cancellation.Token.IsCancellationRequested)
            {

                try
                {
                    // we wait for a client to connect
                    TcpClient endClient = await m_listener.AcceptTcpClientAsync();
                    Log(LogLevel.Debug, $"Client starting connection");
                    
                    // in case we have a max connection limit, we check if we have reached it
                    if (Configuration.MaxConnections != 0 && m_clients.Count >= Configuration.MaxConnections)
                    {
                        // we close the client connection
                        Log(LogLevel.Warn, $"Max connections reached. [{m_clients.Count}]");
                        endClient.Close();
                        continue;
                    }

                    //handle client connection. We dont await this task, because we want to continue listening for new clients
                    _ = Task.Run(() => HandleClientAsync(endClient));
                    
                    
                }
                catch (Exception e)
                {
                    Log(LogLevel.Debug, $"Client connection failed. {e.Message}");
                }
                
            }
        }

        private async Task HandleClientAsync(TcpClient client)
        {
            // we create a new client
            Server.Client c = new Server.Client(client, this);
            c.TcpClient.ReceiveBufferSize = 8192;
            c.TcpClient.SendBufferSize = 8192;
            Log(LogLevel.Info, $"Client connected [{c.Id}]");
            Log(LogLevel.Debug, $"[C{c.Id}] Waiting initial message");
            
            // we wait for the client to send us a message (InitialMessage). If this function returns null, we disconnect the client
            ClientConnectionData data = await c.WaitForConnectionData();
            
            
            if (data == null)
            {
                Log(LogLevel.Debug, $"[C{c.Id}] Client disconnected");
                c.Close();
                return;
            }
            
            Log(LogLevel.Debug, $"[C{c.Id}] Initial message received");
            try
            {
                // we raise the ClientConnecting event
                var eresult = new ClientConnectingEvent(c, data);
                ClientConnecting?.Invoke(this, eresult);
                
                // if the event was cancelled, we disconnect the client
                if (eresult.IsCanceled)
                {
                    Log(LogLevel.Debug, $"[C{c.Id}] Client connection canceled");
                    await c.TcpClient.GetStream().WriteAsync(new byte[] {0});
                    c.Close();
                    return;
                }
                //await c.TcpClient.GetStream().WriteAsync(new byte[] {1});
                // if the event was not canceled, we add the client to the list of connected clients
                m_clients.Add(c.Id, c);
                // and we raise the ClientConnected event
                await c.EndInitialization();
                Log(LogLevel.Debug, $"[C{c.Id}] Client ready");
                ClientConnected?.Invoke(this, c);
                // we start the client handling task
            }
            catch (Exception e)
            {
                Log(LogLevel.Error, $"Client connecting event failed. {e.Message}");
            }
            
        }

        public void RegisterController<T>()  where T : BaseController
        {
            if (IsRunning)
            {
                throw new ServerStateException("Cannot register controller while the server is running.", this);
            }
            m_controllerManager.RegisterController<T>();
        }

        public void RegisterControllers(Assembly assembly)
        {
            if (IsRunning)
            {
                throw new ServerStateException("Cannot register controller while the server is running.", this);
            }

        }

        public void Dispose()
        {
            //Listener.Stop();
        }

        public void Stop()
        {
            //disconnect all clients
            foreach (var client in m_clients.Values)
            {
                client.Disconnect();
            }
            
            //stop listening for new clients
            m_listener.Stop();
            
            //cancel all pending tasks
            m_cancellation.Cancel();
            
            Log(LogLevel.Info, "Server stopped");
        }

        internal void CDisconnect(Server.Client client)
        {
            ClientDisconnected?.Invoke(this, client);
        }
        
        public List<Server.Client> GetClients()
        {
            return m_clients.Values.ToList();
        }
        
        public Server.Client GetClient(int id)
        {
            return m_clients.ContainsKey(id) ? m_clients[id] : null;
        }
        
        public async Task BroadcastInvoke(string method, params object[] args)
        {
            foreach (var client in m_clients.Values.ToList())
            {
                await client.InvokeAsync(method, args);
            }
        }

        #endregion
    }

}