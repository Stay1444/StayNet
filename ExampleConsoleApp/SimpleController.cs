using System.Threading.Tasks;
using StayNet.Common.Attributes;
using StayNet.Common.Controllers;

namespace ExampleConsoleApp
{
    
    public class SimpleController : BaseController
    {
        [Method("message")]
        public async Task Message(string text)
        {
            
        }
        
        
    }
}