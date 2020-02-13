using System;
using System.Reflection;

namespace ProxyGenerator
{
    public abstract class InterceptingDecorator<TService>
    {
        internal static readonly MethodInfo InvokeMethod =
            typeof(InterceptingDecorator<TService>).GetMethod(nameof(Invoke), BindingFlags.NonPublic | BindingFlags.Instance);

        private readonly TService _service;
        private readonly MethodInfo[] _methodInfos;

        protected InterceptingDecorator(TService service)
        {
            _service = service;
            _methodInfos = service.GetType().GetInterfaceMap(typeof(TService)).TargetMethods;
        }
        
        protected object Invoke(int methodIndex, object[] args, Type[] typeArgs)
        {
            var methodInfo = _methodInfos[methodIndex];

            if (typeArgs != null)
                methodInfo = methodInfo.MakeGenericMethod(typeArgs);

            var call = new Call(_service, methodInfo, args);
            return Intercept(call);
        }
        
        protected abstract object Intercept(ICall call);
    }
}