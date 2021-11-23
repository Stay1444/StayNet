using System.Threading;

namespace StayNet.Common.Entities
{
    internal class MethodInvokeManager
    {
        
        private MethodInvokeManager(){}

        private CancellationToken CToken;
        private String MethodName;
        private Object[] Parameters;
        
        public static MethodInvokeManager Create(CancellationToken ct, string messageId, object[] invArgs)
        {
            MethodInvokeManager manager = new MethodInvokeManager();
            manager.CToken = ct;
            manager.MethodName = messageId;
            manager.Parameters = invArgs;
            return manager;
        }

        public bool  TryInvoke()
        {
            
            return false;
        }
        
    }
}