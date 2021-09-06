using System.Reflection;

namespace ProxyGenerator
{
    public interface ICall
    {
        /// <summary>
        /// The method that is being called.
        /// </summary>
        MethodInfo Method { get; }
        /// <summary>
        /// The arguments used to call the method.
        /// </summary>
        object[] Arguments { get; }
        
        /// <summary>
        /// Executes the original method call and returns its return value.
        /// </summary>
        /// <returns>An <see cref="ICallResult"/> containing the return value of the original method call.</returns>
        ICallResult Continue();
    }
}