using System;

namespace ProxyGenerator
{
    public interface IProxyGenerator
    {
        Type GenerateProxy<TService, TProxy>() where TProxy : InterceptingDecorator<TService>;
    }
}