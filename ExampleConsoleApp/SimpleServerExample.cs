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
            server.ClientConnecting += (sender, e) => 
            {
                Console.WriteLine($"Client connecting: {e.ConnectionData.ReadString()}");
            };

            new Thread(() =>
            {

                while (true)
                {
                    Thread.Sleep(100);
                    if (server.GetClients().Count > 0)
                    {
                        Console.WriteLine($"Connections: {server.GetClients().Count}");
                        foreach (var client in server.GetClients())
                        {
                            Console.WriteLine($"Client: {client.Id} Ping: {client.Ping}");
                        }
                    }
                }
                
            }).Start();

        }
        
    }
}