[//]: # (./src/HostBridge.WindowsService/README.md)

# HostBridge.WindowsService

💡 *“Windows Services that behave like modern .NET background services.”*

This package provides a base class for **Windows Services** with DI, graceful startup/shutdown, and proper lifetime handling.

---

## API Surface

- `HostBridgeServiceBase` – base class wrapping `ILegacyHost` lifetimes

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
````

**Program.cs**

```csharp
static void Main(string[] args)
{
    using var service = new MyWindowsService();

#if DEBUG
    service.Start(args);
    Console.ReadLine();
    service.Stop();
#else
    ServiceBase.Run(service);
#endif

    new HostBridgeVerifier()
        .Add(() => WindowsServiceChecks.VerifyWindowsService(!Environment.UserInteractive))
        .Log(HB.Get<ILogger<Program>>());
}
```

---

## How it works

* `OnStart` builds host and calls `StartAsync`.
* `OnStop`/`OnShutdown` call `StopAsync` with timeout then dispose.
* Supports running as console in DEBUG for easier dev.

---

## Diagnostics Tip

Verifier warns if running in console vs. SCM. Logs instead of failing fast.

---

## Notes

* Scoped lifetimes flow correctly through hosted services.
* Shutdown timeout defaults to 30s; override `ShutdownTimeout` if needed.
* See also:
    * [HostBridge.Core](../HostBridge.Core/README.md) – ambient accessor
    * [HostBridge.Diagnostics](../HostBridge.Diagnostics/README.md) – wiring checks