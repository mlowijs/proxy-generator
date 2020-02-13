using System.Reflection;

namespace ProxyGenerator
{
    public class Call : ICall
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

        public object Continue()
        {
            return Method.Invoke(_instance, Arguments);
        }
    }
}