[//]: # (./src/HostBridge.Abstractions/README.md)

# HostBridge.Abstractions

💡 *“The contracts so everything else hangs together.”*

This package defines the small set of interfaces and types used across all HostBridge libraries.  
They are intentionally minimal and netstandard2.0-compatible.

---

## API Surface

- `ILegacyHost` – root host abstraction (`Services`, `StartAsync`, `StopAsync`)
- `IHostedService` – background service contract (start/stop)
- `HostContext` – provides `IConfiguration` + `IHostEnvironment`

---

## Wiring

Most consumers do **not** implement `ILegacyHost` themselves. Instead, they use `LegacyHostBuilder` in `HostBridge.Core`.  
That said, here’s a minimal example:

```csharp
public sealed class LegacyHost : ILegacyHost, IDisposable
{
    public LegacyHost(IServiceProvider services) => ServiceProvider = services;
    public IServiceProvider ServiceProvider { get; }
    public Task StartAsync(CancellationToken ct) => Task.CompletedTask;
    public Task StopAsync(CancellationToken ct) => Task.CompletedTask;
    public void Dispose() => (ServiceProvider as IDisposable)?.Dispose();
}
```

**Hosted service sample:**

```csharp
public sealed class HeartbeatService : IHostedService
{
    private Timer? _timer;

    public Task StartAsync(CancellationToken ct)
    {
        _timer = new Timer(_ => Console.WriteLine("tick"), null, 0, 1000);
        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken ct)
    {
        _timer?.Dispose();
        return Task.CompletedTask;
    }
}
```

---

## How it works

* `ILegacyHost` is the glue all adapters call into.
* `IHostedService` gives you background work across app types.
* `HostContext` carries config + environment name into `LegacyHostBuilder`.

---

## Diagnostics Tip

Nothing to check here — wiring validation happens at the adapter level.
See [HostBridge.Diagnostics](../HostBridge.Diagnostics/README.md).

---

## Notes

* Public API is kept netstandard2.0-compatible.
* Keep implementations simple; prefer using `HostBridge.Core` for real hosts.
* See also:
    * [HostBridge.Core](../HostBridge.Core/README.md) – `LegacyHostBuilder`, HB accessor
