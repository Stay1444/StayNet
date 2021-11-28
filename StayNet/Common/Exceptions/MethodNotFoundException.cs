namespace StayNet.Common.Exceptions
{
    public class MethodNotFoundException : Exception
    {
        public readonly string MethodName;
        public MethodNotFoundException(string message, string method) : base(message)
        {
            this.MethodName = method;
        }
    }
}