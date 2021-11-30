using System.Threading.Tasks;
using System.Timers;
using StayNet.Common.Attributes;
using StayNet.Common.Controllers;

namespace ExampleConsoleApp
{
    
    public class SimpleController : BaseController
    {
        private static int total;
        private static int count;
        private static System.Timers.Timer timer = new System.Timers.Timer(1500);
        [Method("Hi")]
        public async Task Message(string i, int t)
        {
            count = t;
            total+= 1;
            Console.WriteLine($"{i}");
        }

        public static void test()
        {
            timer.Start();
            timer.AutoReset = true;
            timer.Elapsed += (sender, args) =>
            {
                if (total >= count)
                {
                    Console.WriteLine("Done");
                    total = 0;
                    SimpleServerExample.TestRun(10000);
                }
                Console.WriteLine(total + " " + count);
            };
        }
        
        
    }
}