# HostBridge.Abstractions

💡 *“The contracts so everything else hangs together.”*

This is the small set of interfaces and types used everywhere else:

* `ILegacyHost` — exposes an `IServiceProvider` and lifecycle methods (`StartAsync`, `StopAsync`).
* `IHostedService` — classic version of the background service abstraction.
* `HostContext` — passes configuration + environment info during setup.

### Minimal example

```csharp
public sealed class LegacyHost : ILegacyHost, IDisposable
{
    public LegacyHost(IServiceProvider sp) => ServiceProvider = sp;
    public IServiceProvider ServiceProvider { get; }
    public Task StartAsync(CancellationToken ct) => Task.CompletedTask;
    public Task StopAsync(CancellationToken ct) => Task.CompletedTask;
    public void Dispose() => (ServiceProvider as IDisposable)?.Dispose();
}
```

### Hosted service sample

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

### Notes

* Nothing fancy here, just the glue.
* Most folks never implement `ILegacyHost` directly — you just wrap your DI container and hand it to `HB`, `AspNetBootstrapper`, or `HostBridgeWcf`.
