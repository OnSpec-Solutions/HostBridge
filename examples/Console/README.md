[//]: # (./examples/Console/README.md)

# Console Example

💡 *“Because sometimes you just need a loop and a logger.”*

This project demonstrates wiring HostBridge in a simple console app.

---

## Wiring

**Program.cs**

```csharp
static async Task Main()
{
    var host = new LegacyHostBuilder()
        .ConfigureAppConfiguration(cfg => cfg.AddHostBridgeAppConfig())
        .ConfigureLogging(lb => lb.AddConsole())
        .ConfigureServices((_, services) =>
        {
            services.AddOptions();
            services.AddHostedService<HeartbeatService>();
            services.AddSingleton<IWorker, Worker>();
            services.AddScoped<IOperation, Operation>();
        })
        .Build();

    // Initialize the accessor once
    HB.Initialize(host);

    // Example: run a scoped “operation” in console land
    using (HB.BeginScope())
    {
        var worker = HB.Get<IWorker>();
        await worker.RunAsync();
    }

    // Or run until canceled with a shutdown grace period
    using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(10));
    await host.RunAsync(cts.Token, shutdownTimeout: TimeSpan.FromSeconds(5));
}
```

---

## How it works

* `LegacyHostBuilder` sets up config + DI.
* `HB.Initialize(host)` makes the host accessible statically.
* `HB.BeginScope()` creates a disposable ambient scope.
* Correlation ensures all logs in the loop share a `CorrelationId`.

---

## Diagnostics Tip

Always add a verifier in dev/test; log results in prod.

---

## Notes

* Scoped services are unique per run scope.
* Use correlation per loop/job iteration for traceability.