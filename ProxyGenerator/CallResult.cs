namespace ProxyGenerator
{
    internal class CallResult : ICallResult
    {
        public object ReturnValue { get; }

        public CallResult(object result)
        {
            ReturnValue = result;
        }
    }
}