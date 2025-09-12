[//]: # (./src/HostBridge.Core/README.md)

# HostBridge.Core

💡 *“A static accessor that doesn’t make you hate yourself.”*

This package provides `LegacyHostBuilder` and the `HB` static accessor for console apps, services, and other edge cases.

---

## API Surface

- `LegacyHostBuilder` – configure services, logging, config, and build an `ILegacyHost`
- `HB.Initialize(host)` – set the root host once
- `HB.Root` – root provider (composition root)
- `HB.Current` – ambient provider (flows via AsyncLocal)
- `HB.Get<T>()` / `HB.TryGet<T>()` – resolve from current provider
- `HB.CreateScope()` / `HB.BeginScope()` – manual or ambient scope creation

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

    using (HB.BeginScope())
    {
        var svc = HB.Get<IMyScoped>();
        Console.WriteLine(svc.ToString());
    }

    using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(10));
    await host.RunAsync(cts.Token, shutdownTimeout: TimeSpan.FromSeconds(5));
}
```

---

## How it works

* `LegacyHostBuilder` mirrors the modern .NET Core host builder.
* `HB` is a safe static accessor for edge cases:

    * Ambient scope flows via AsyncLocal
    * Root access is guarded (throws if not initialized)
* Scoped services are disposed when their scope ends.

---

## Diagnostics Tip

Use [HostBridge.Diagnostics](../HostBridge.Diagnostics/README.md) to verify bootstrap/init is correct.
Especially important in tests and console apps.

---

## Notes

* `HB` should be used at app edges only; prefer constructor injection in real services.
* Correlation scopes (`Correlation.Begin`) nest cleanly inside `HB.BeginScope()`.
* See also:
    * [HostBridge.Options.Config](../HostBridge.Options.Config/README.md) – config binding
    * [HostBridge.Diagnostics](../HostBridge.Diagnostics/README.md) – wiring checks