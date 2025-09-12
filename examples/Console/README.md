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
        .ConfigureServices((ctx, services) =>
        {
            services.AddOptions();
            services.AddScoped<IMyScoped, MyScoped>();
            services.AddHostedService<Worker>();
        })
        .Build();

    HB.Initialize(host);

    // Diagnostics – fail fast in dev/test
    new HostBridgeVerifier()
        .Add(AspNetChecks.VerifyAspNet) // or project-appropriate checks
        .ThrowIfCritical();

    using (Correlation.Begin(HB.Get<ILogger<Program>>()))
    {
        var worker = HB.Get<Worker>();
        await worker.DoWork();
    }

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