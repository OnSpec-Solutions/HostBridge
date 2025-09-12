# HostBridge.Health

💡 *“Tiny health check primitives so ops can sleep at night.”*

You don’t need a giant diagnostics subsystem. You just need a way for parts of your app to say *“I’m ok”* or *“I’m on fire.”*

### Primitives

* `HealthStatus` — Healthy, Degraded, Unhealthy
* `HealthResult` — status + description (`Healthy()`, `Degraded()`, `Unhealthy()`)
* `IHealthContributor` — implement this to report health for a subsystem

### Example contributor

```csharp
public sealed class DatabaseHealth : IHealthContributor
{
    public string Name => "database";
    public Task<HealthResult> Check()
    {
        // pretend to ping the DB
        return Task.FromResult(HealthResult.Healthy("ping ok"));
    }
}
```

### How you use it

```csharp
var contributors = HB.Current.GetServices<IHealthContributor>();
var results = await Task.WhenAll(contributors.Select(c => c.Check()));

foreach (var r in results)
    Console.WriteLine($"{r.Status}: {r.Description}");
```

### Notes

* Doesn’t host an endpoint. You decide whether to surface results at `/health`, in logs, or via a diagnostics dashboard.
* Keeps the API surface small and unopinionated — just enough to build what you need.
