using System;
using System.Linq;
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

            if (typeArgs.Any())
                methodInfo = methodInfo.MakeGenericMethod(typeArgs);

            var call = new Call(_service, methodInfo, args);
            return Intercept(call).ReturnValue;
        }
        
        /// <summary>
        /// Intercepts a method call.
        /// </summary>
        /// <param name="call">An object containing information about the method call.</param>
        /// <returns>The return value of the Continue method.</returns>
        protected abstract ICallResult Intercept(ICall call);
    }
}