using System.Linq;
using System.Net;
using System.Threading;
using StayNet;
using StayNet.Common.Enums;

namespace ExampleConsoleApp
{
    public class SimpleServerExample
    {
        
        public static void Run()
        {
            var config = new StayNetServerConfiguration
            {
                Host = IPAddress.Any,
                Port = 1444,
                MaxConnections = 10,
                LogLevel = LogLevel.Debug,
                Logger = new ConsoleLogger()
            };
            
            var server = new StayNetServer(config);
            server.RegisterController<SimpleController>();
            server.Start();

            server.ClientConnecting += (server, ev) =>
            {
                if (ev.ConnectionData.ReadString() != "CARA RANA")
                {
                    Console.WriteLine("UNAUTHORIZED");
                    ev.Cancel();
                }
            };
            
            new Thread(() =>
            {
                while (true)
                {
                    Thread.Sleep(1000);

                    if (server.GetClients().Count > 0)
                    {
                        Console.WriteLine("Clients: " + server.GetClients().Count);
                        server.GetClients().FirstOrDefault()?.InvokeAsync("", 1, "hi", config);
                    }
                    
                }
            }).Start();

        }
        
    }
}