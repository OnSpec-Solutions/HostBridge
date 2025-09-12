[//]: # (./examples/WindowsService/README.md)

# Windows Service Example

💡 *“Windows Services that behave like modern .NET background services.”*

This project demonstrates a DI-driven Windows Service using HostBridge.

---

## Wiring

**MyWindowsService.cs**

```csharp
public sealed class MyWindowsService : HostBridgeServiceBase
{
    protected override ILegacyHost BuildHost()
    {
        var services = new ServiceCollection();
        services.AddOptions();
        services.AddSingleton<IMyWorker, MyWorker>();
        return new LegacyHost(services.BuildServiceProvider());
    }
}
```

**Program.cs**

```csharp
static void Main(string[] args)
{
    using var svc = new MyWindowsService();

#if DEBUG
    svc.Start(args); Console.ReadLine(); svc.Stop();
#else
    ServiceBase.Run(svc);
#endif

    new HostBridgeVerifier()
        .Add(() => WindowsServiceChecks.VerifyWindowsService(!Environment.UserInteractive))
        .Log(HB.Get<ILogger<Program>>());
}
```

---

## How it works

* Base class handles graceful start/stop.
* Scoped lifetimes flow through hosted services.
* Correlation can be started per background job loop.