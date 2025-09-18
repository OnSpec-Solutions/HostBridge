[//]: # (./examples/WindowsService/README.md)

# Windows Service Example

💡 *“Windows Services that behave like modern .NET background services.”*

This project demonstrates a DI-driven Windows Service using HostBridge.

---

## Wiring

**MyWindowsService.cs**

```csharp
public class MyWindowsService : HostBridgeServiceBase
{
    protected override ILegacyHost BuildHost()
    {
        var host = new LegacyHostBuilder()
            .ConfigureLogging(lb => lb.AddConsole())
            .ConfigureServices((ctx, services) =>
            {
                services.AddOptions();
                services.AddHostedService<HeartbeatService>();
                services.AddSingleton<IWorker, Worker>();
            })
            .Build();
        
        HB.Initialize(host);

        return host;
    }
}
```

**Program.cs**

```csharp
static void Main(string[] args)
{
    using var service = new MyWindowsService();
            
#if DEBUG
    service.Start(args);
            
    new HostBridgeVerifier()
        .Add(() => WindowsServiceChecks.VerifyWindowsService(runningAsService: !Environment.UserInteractive))
        .Log(HB.Get<ILogger<MyWindowsService>>());
    
    Console.ReadLine();
    service.Stop();
#else
    ServiceBase.Run(service);
#endif
}
```

---

## How it works

* Base class handles graceful start/stop.
* Scoped lifetimes flow through hosted services.
* Correlation can be started per background job loop.