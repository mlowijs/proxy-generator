using System;

namespace ProxyGenerator
{
    public interface IProxyGenerator
    {
        Type GenerateProxy<TService, TProxy>() where TProxy : InterceptingDecorator<TService>;
        TService CreateProxy<TService, TProxy>(params object[] args) where TProxy : InterceptingDecorator<TService>;
    }
}