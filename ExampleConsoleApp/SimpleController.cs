using System.Threading;
using System.Threading.Tasks;
using System.Timers;
using StayNet.Common.Attributes;
using StayNet.Common.Controllers;

namespace ExampleConsoleApp
{
    
    public class SimpleController : BaseController
    {
        private static int total;
        [Method("Hi")]
        public async Task Message(string i, int t)
        {
            
            //increment total thread safe
            Interlocked.Increment(ref total);
            if (total >= 2500)
            {
            }
            Console.WriteLine($"{i} {total}");
            
        }

        public static void Clear()
        {
            total = 0;
        }        
        
    }
}