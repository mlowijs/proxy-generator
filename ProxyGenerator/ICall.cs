using System.Reflection;

namespace ProxyGenerator
{
    public interface ICall
    {
        MethodInfo Method { get; }
        object[] Arguments { get; }
        
        object Continue();
    }
}