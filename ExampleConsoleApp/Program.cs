
global using System;
using System.Linq;
using System.Net;
using System.Threading;
using ExampleConsoleApp;
using StayNet;
Console.WriteLine("Hello World!");

byte[] source = new byte[282];
byte[] dest = new byte[282];

Array.Copy(source, dest, 282);

SimpleServerExample.Run();
Thread.Sleep(2500);
SimpleClientExample.Run(new IPEndPoint(IPAddress.Parse("127.0.0.1"), 1444));
while (true)
{
    Console.ReadLine();
    SimpleController.Clear();
    SimpleServerExample.TestRun(100000);
}
