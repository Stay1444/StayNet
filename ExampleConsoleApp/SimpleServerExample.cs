using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Threading;
using Newtonsoft.Json;
using StayNet;
using StayNet.Common.Enums;

namespace ExampleConsoleApp
{
    public class SimpleServerExample
    {
        public static StayNetServer server;
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
            
             server = new StayNetServer(config);
            server.Start();
            server.ClientConnecting += (sender, e) => 
            {
                Console.WriteLine($"Client connecting: {e.ConnectionData.ReadString()}|");
            };

            server.ClientConnected += (sender, e) =>
            {
                Console.WriteLine($"Client connected|");
            };

            
            
        }
        
        public static void TestRun(int t)
        {
            Console.WriteLine($"Sending message to {server.GetClients().Count} clients");
            Stopwatch w = new Stopwatch();
            w.Start();
                for (int i = 0; i<t; i++)
                {
                        
                     server.GetClients().First().InvokeAsync("Hi",Guid.NewGuid().ToString(), t);
                        
                }

                Console.WriteLine($"Sent message to {server.GetClients().Count} clients in {w.ElapsedMilliseconds}ms");
        }
        
        
    }
}