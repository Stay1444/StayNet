using System.Linq;
using System.Threading.Tasks;
using StayNet.Common.Attributes;

namespace StayNet.Common.Controllers
{
    public abstract class BaseController
    {
        
        public virtual async Task BeforeMethodInvoke(object context)
        {
            await Task.CompletedTask;
        }

        
    }
}