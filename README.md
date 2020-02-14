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

// Startup.cs (SimpleInjector example, inject IMyService whereever you need it)
//
// SimpleInjector handles injecting the concrete MyService into the generated proxy
// through the RegisterDecorator mechanism
container.Register<IMyService, MyService>();

var proxyGenerator = new CilProxyGenerator();
var proxyType = proxyGenerator.GenerateProxy<IMyService, MyDecorator>();

container.RegisterDecorator(typeof(IMyService), proxyType);

// ... or create an instance of the proxy directly, if required
var proxyGenerator = new CilProxyGenerator();
var myService = proxyGenerator.CreateProxy<IMyService, MyDecorator>(new MyService());
myService.MyMethod();
```