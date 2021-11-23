using System.Net;
using System.Threading;
using StayNet;

namespace ExampleConsoleApp
{
    public class SimpleClientExample
    {
        public static void Run(IPEndPoint end)
        {
            var config = new StayNetClientConfiguration();
            config.Logger = new ConsoleLogger();
            var client = new StayNetClient(end, config);
            client.Connect("CARA RANA");

            Thread.Sleep(2500);
            client.Disconnect();

        }
    }
}