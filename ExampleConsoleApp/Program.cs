
global using System;
using System.Linq;
using System.Net;
using System.Threading;
using ExampleConsoleApp;
using StayNet;
using StayNet.Server.Controllers;

Console.WriteLine("Hello World!");

SimpleServerExample.Run();
Thread.Sleep(2500);
SimpleClientExample.Run(new IPEndPoint(IPAddress.Parse("127.0.0.1"), 1444));

while (true)
{
    Thread.Sleep(5000);
}
/*
 *
 * THIS PROGRAM DOESNT REALLY DO ANYTHING. BUT YOU CAN CHECK THE EXAMPLES. 
 * 
*/

