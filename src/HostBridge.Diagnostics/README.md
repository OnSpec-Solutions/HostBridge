[//]: # (./src/HostBridge.Diagnostics/README.md)

# HostBridge.Diagnostics

💡 *“Find miswirings before they find you.”*

This package provides verifier utilities that catch common HostBridge wiring mistakes.  
Use them to **fail fast in dev/test** and **log in production** so you don’t run with half-wired scopes.

---

## API Surface

- `HostBridgeVerifier` – orchestrates checks
  - `.Add(check)` – add one or more checks
  - `.ThrowIfCritical()` – fail fast on errors/criticals
  - `.Log(logger)` – log all results with severity
- `AspNetChecks.VerifyAspNet()` – ensures:
  - `AspNetBootstrapper.Initialize(host)` called
  - `AspNetRequestScopeModule` registered in `web.config`
  - MVC/Web API resolvers are HostBridge resolvers
- `WcfChecks.VerifyWcf()` – ensures:
  - `HostBridgeWcf.Initialize(host)` called
  - recommends `DiServiceHostFactory` in `.svc` or config
- `WindowsServiceChecks.VerifyWindowsService(runningAsService)` – checks service vs console mode

---

## Wiring

**ASP.NET Global.asax.cs**

```csharp
protected void Application_Start()
{
    var host = new LegacyHostBuilder()
        .ConfigureServices((ctx, s) => s.AddScoped<IMyScoped, MyScoped>())
        .Build();

    AspNetBootstrapper.Initialize(host);

    new HostBridgeVerifier()
        .Add(AspNetChecks.VerifyAspNet)
        .ThrowIfCritical(); // dev/test
}
```

**WCF Global.asax.cs**

```csharp
protected void Application_Start()
{
    var host = new LegacyHostBuilder()
        .ConfigureServices((ctx, s) => s.AddScoped<IMyScoped, MyScoped>())
        .Build();

    HostBridgeWcf.Initialize(host);

    new HostBridgeVerifier()
        .Add(WcfChecks.VerifyWcf)
        .Log(HB.Get<ILogger<Global>>()); // log in prod
}
```

**Windows Service Program.cs**

```csharp
new HostBridgeVerifier()
    .Add(() => WindowsServiceChecks.VerifyWindowsService(!Environment.UserInteractive))
    .Log(HB.Get<ILogger<Program>>());
```

---

## How it works

* Each check returns `DiagnosticResult` objects with `Code`, `Severity`, `Message`, and optional `Fix`.
* Verifier aggregates results and either:

    * Throws `InvalidOperationException` if any `Error`/`Critical` (dev/test), or
    * Logs results with severity (prod).
* Examples of what it catches:

    * Bootstrapper not initialized
    * Missing ASP.NET module registration
    * Wrong resolver in MVC/Web API
    * WCF service not using HostBridge factory

---

## Diagnostics Tip

* Run `.ThrowIfCritical()` in dev/test to catch miswiring early.
* Run `.Log(logger)` in production to avoid crashing the app.
* Checks do **not** validate correlation; that’s controlled by `CorrelationOptions`.

---

## Example Messages

* **HB-ASP-001 (Critical)**
  *AspNetBootstrapper.Initialize(host) has not been called.*
  **Fix:** Call `AspNetBootstrapper.Initialize(host)` in `Global.asax Application_Start`.

* **HB-ASP-002 (Error)**
  *Per-request scope is missing.*
  **Fix:** Add `<add name="HostBridgeRequestScope" type="HostBridge.AspNet.AspNetRequestScopeModule" />` in `web.config`.

* **HB-MVC-001 (Warning)**
  *MVC DependencyResolver is not HostBridge’s.*
  **Fix:** `DependencyResolver.SetResolver(new MvcDependencyResolver());`

* **HB-WCF-001 (Critical)**
  *HostBridgeWcf.Initialize(host) has not been called.*
  **Fix:** Call `HostBridgeWcf.Initialize(host)` in `Application_Start`.

---

## Notes

* Use in all example apps — don’t ship demos without verifier wiring.
* Extend with your own checks for critical config keys if needed.
* See also:
    * [HostBridge.Core](../HostBridge.Core/README.md) – HB accessor
    * [HostBridge.AspNet](../HostBridge.AspNet/README.md) – request scope module
    * [HostBridge.Wcf](../HostBridge.Wcf/README.md) – per-operation scopes
