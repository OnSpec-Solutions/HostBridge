[//]: # (./src/HostBridge.Health/README.md)

# HostBridge.Health

💡 *“Tiny health check primitives so ops can sleep at night.”*

This package provides minimal primitives for reporting health.  
It does not host an endpoint — you choose how to expose results.

---

## API Surface

- `HealthStatus` – `Healthy`, `Degraded`, `Unhealthy`
- `HealthResult` – status + description
    - `HealthResult.Healthy("ok")`
    - `HealthResult.Degraded("slow")`
    - `HealthResult.Unhealthy("down")`
- `IHealthContributor` – implement this to report subsystem health

---

## Wiring

**Contributor Example**

```csharp
public sealed class DatabaseHealth : IHealthContributor
{
    public string Name => "database";

    public Task<HealthResult> Check()
    {
        // pretend to ping DB
        return Task.FromResult(HealthResult.Healthy("ping ok"));
    }
}
```

**Registration**

```csharp
services.AddSingleton<IHealthContributor, DatabaseHealth>();
services.AddSingleton<IHealthContributor, QueueHealth>();
```

---

## How it works

* Contributors return a `HealthResult`.
* You aggregate results and decide overall status.
* Status typically determines HTTP 200 vs. 503 in an endpoint.

---

## Diagnostics Tip

Health contributors are not checked by `HostBridge.Diagnostics`.
Use them to implement subsystem health, not wiring checks.

---

## Example Endpoint

**Web API 2 controller**

```csharp
[RoutePrefix("")]
public sealed class HealthController : ApiController
{
    private readonly IEnumerable<IHealthContributor> _contributors;
    public HealthController(IEnumerable<IHealthContributor> contributors) => _contributors = contributors;

    [HttpGet, Route("health")]
    public async Task<IHttpActionResult> Get()
    {
        var results = await Task.WhenAll(_contributors.Select(c => c.Check()));
        var overall = results.Any(r => r.Status == HealthStatus.Unhealthy)
            ? HealthStatus.Unhealthy
            : results.Any(r => r.Status == HealthStatus.Degraded)
                ? HealthStatus.Degraded
                : HealthStatus.Healthy;

        return Content(
            overall == HealthStatus.Healthy ? HttpStatusCode.OK : HttpStatusCode.ServiceUnavailable,
            new { status = overall.ToString(), results = results });
    }
}
```

---

## Notes

* Keep contributors small and focused (DB, queue, cache, etc.).
* Aggregate at app edges (controller, handler, middleware).
* See also:
    * [HostBridge.Diagnostics](../HostBridge.Diagnostics/README.md) – wiring verifier