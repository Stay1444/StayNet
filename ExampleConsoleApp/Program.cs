
global using System;
using System.Linq;
using System.Net;
using System.Threading;
using ExampleConsoleApp;
using StayNet;

Console.WriteLine("Hello World!");

SimpleServerExample.Run();
Thread.Sleep(2500);
SimpleClientExample.Run(new IPEndPoint(IPAddress.Parse("127.0.0.1"), 1444));

while (true)
{
    Thread.Sleep(5000);
}
