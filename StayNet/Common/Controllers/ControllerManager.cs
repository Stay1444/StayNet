using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using StayNet.Common.Attributes;

namespace StayNet.Common.Controllers
{
    internal class ControllerManager
    {
        public class ControllerManagerNode
        {
            public string MethodId;
            public Type Class;
            public MethodInfo Method;
            public ParameterInfo[] Parameters;
        }
        
        internal Dictionary<String, ControllerManagerNode> ControllerMethods =
            new Dictionary<string, ControllerManagerNode>();
        internal void RegisterController<T>()
        {
            registerMethods<T>();
        }

        private void registerMethods<T>()
        {
            var methods = typeof(T).GetMethods();

            foreach (var mt in methods)
            {

                if (!mt.GetCustomAttributes<Method>().Any())
                    continue;
                
                var methodId = mt.GetCustomAttribute<Method>().MethodId;
                
                if (methodId == null)
                    continue;
                
                if (methodId.Trim().Length == 0)
                    continue;
                
                if (ControllerMethods.ContainsKey(methodId))
                    continue;
                
                
                var parameterTypes = mt.GetParameters().ToArray();

                ControllerManagerNode node = new();
                node.MethodId = methodId;
                node.Class = typeof(T);
                node.Method = mt;
                node.Parameters = parameterTypes;
                
                ControllerMethods.Add(methodId, node);

            }   
        }
        
        internal bool IsValidMethod(string methodId)
        {
            return ControllerMethods.ContainsKey(methodId);
        }
        
        internal bool CanInvokeMethod(string methodId, object[] parameters)
        {
            if (!ControllerMethods.ContainsKey(methodId))
                return false;
            
            var node = ControllerMethods[methodId];
            
            if (node.Parameters.Length != parameters.Length)
                return false;
            
            for (int i = 0; i < node.Parameters.Length; i++)
            {
                if (node.Parameters[i].ParameterType != parameters[i].GetType())
                    return false;
            }
            
            return true;
        }
        
        internal async Task InvokeMethod(string methodId, object[] parameters)
        {
            if (!ControllerMethods.ContainsKey(methodId))
                return;
            
            var node = ControllerMethods[methodId];
            await (Task)node.Method.Invoke(node.Class, parameters);
            return;
        }
        
    }
}