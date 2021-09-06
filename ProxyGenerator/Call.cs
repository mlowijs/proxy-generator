using System.Reflection;

namespace ProxyGenerator
{
    internal class Call : ICall
    {
        private readonly object _instance;

        public Call(object instance, MethodInfo methodInfo, object[] args)
        {
            _instance = instance;
            
            Method = methodInfo;
            Arguments = args;
        }
        
        public MethodInfo Method { get; }
        public object[] Arguments { get; }

        public ICallResult Continue()
        {
            var result = Method.Invoke(_instance, Arguments);
            
            return new CallResult(result);
        }
    }
}