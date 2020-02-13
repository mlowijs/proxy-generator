Generates proxies to intercept method calls to classes.

```csharp
interface IMyService
{
    void MyMethod();
}

class MyService : IMyService
{
    public void MyMethod()
    {
        // ...
    }
}

class MyDecorator : InterceptingDecorator<IMyService>
{
    public MyDecorator(IMyService service) : base(service)
    {
        // Inject more services here perhaps
    }

    protected override object Intercept(ICall call)
    {
        // Do things here before you execute the call
        Console.WriteLine($"Before calling {call.Method.Name}");
        
        var returnValue = call.Continue();

        // Do things here after you execute the call
        Console.WriteLine($"After calling {call.Method.Name}");

        return returnValue;
    }
}

// ...

IProxyGenerator proxyGenerator = new CilProxyGenerator();

var proxyType = proxyGenerator.GenerateProxy<IMyService, MyDecorator>();

// Register the proxy as a decorator in e.g. SimpleInjector
container.Register<IMyService, MyService>();
container.RegisterDecorator(typeof(IMyService), proxyType);
```