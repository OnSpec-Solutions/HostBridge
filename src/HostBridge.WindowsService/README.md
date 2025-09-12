# HostBridge.WindowsService

💡 *“Windows Services that behave like modern .NET background services.”*

This package gives you a base class that wraps DI + host lifetimes. You get non-blocking startup and graceful shutdowns.

### Wire-up

```csharp
public sealed class MyWindowsService : HostBridgeServiceBase
{
    protected override ILegacyHost BuildHost()
    {
        var services = new ServiceCollection();
        services.AddOptions();
        services.AddSingleton<IMyWorker, MyWorker>();
        var sp = services.BuildServiceProvider();
        return new LegacyHost(sp);
    }
}
```

**Program.cs**

```csharp
static class Program
{
    static void Main(string[] args)
    {
        using var service = new MyWindowsService();
#if DEBUG
        service.Start(args); // run as console for dev
        Console.ReadLine();
        service.Stop();
#else
        ServiceBase.Run(service);
#endif
    }
}
```

### Behavior

* `OnStart` builds the host and calls `StartAsync`.
* `OnStop` / `OnShutdown` stop the host with a 30s timeout and dispose it.
* Override `ShutdownTimeout` if your services need more time.
