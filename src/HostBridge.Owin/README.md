[//]: # (./HostBridge.Owin/README.md)

# HostBridge.Owin

💡 *“System.Web + OWIN living together, with DI that actually works.”*

This adapter adds **per-request DI scopes** to the OWIN pipeline and a **Web API 2 resolver** that
prefers the OWIN scope (with safe fallbacks). It’s the missing glue when you run OWIN and
System.Web side-by-side or self-host Web API 2.

---

## API Surface

- `UseHostBridgeRequestScope(this IAppBuilder app)`  
  OWIN middleware that creates an `IServiceScope` per request and exposes it in the OWIN env. :contentReference[oaicite:0]{index=0}
- `WebApiOwinAwareResolver`  
  Web API 2 `IDependencyResolver` that resolves from the OWIN scope, falls back to the
  System.Web request scope, and finally the root provider if needed. :contentReference[oaicite:1]{index=1}

---

## Wiring

**Startup.cs**

```csharp
public class Startup
{
    public void Configuration(IAppBuilder app)
    {
        var host = new LegacyHostBuilder()
            .ConfigureServices((ctx, services) =>
            {
                services.AddOptions();
                services.AddScoped<IMyScoped, MyScoped>();
            })
            .Build();

        // Make root provider available to both System.Web and OWIN paths
        AspNetBootstrapper.Initialize(host);

        // OWIN branch: per-request scope for Web API pipeline
        app.UseHostBridgeRequestScope();

        var config = new HttpConfiguration();
        config.DependencyResolver = new HostBridge.Owin.WebApiOwinAwareResolver();
        config.MapHttpAttributeRoutes();
        app.UseWebApi(config);

        // Diagnostics – fail fast in dev/test
        new HostBridgeVerifier()
            .Add(AspNetChecks.VerifyAspNet)
            .ThrowIfCritical();
    }
}
```

**web.config** (keep System.Web modules for WebForms/MVC paths)

```xml
<system.webServer>
  <modules>
    <add name="HostBridgeRequestScope" type="HostBridge.AspNet.AspNetRequestScopeModule" />
    <add name="HostBridgeCorrelation"  type="HostBridge.AspNet.CorrelationHttpModule" />
  </modules>
</system.webServer>
```

---

## How it works

* **OWIN middleware**: creates an `IServiceScope` per request and stashes it in the OWIN
  environment under a shared key; disposes it after the pipeline completes.&#x20;
* **Resolver order (Web API 2)**:

    1. OWIN request scope (from OWIN env)
    2. System.Web request scope (from the ASP.NET module)
    3. Root provider (only if neither scope exists; throws if root is missing)&#x20;
* Result: `AddScoped` means **per request** in both OWIN and System.Web branches, with clean disposal and no cross-request bleed.

---

## Diagnostics Tip

Add the verifier at startup to catch missing bootstraps or modules:

```csharp
new HostBridgeVerifier()
    .Add(AspNetChecks.VerifyAspNet)
    .Log(HB.Get<ILogger<Startup>>()); // or ThrowIfCritical() in dev/test
```

---

## Notes

* Keep the **System.Web modules** even in OWIN apps so WebForms/MVC continue to get proper request scopes.
* If you self-host (no System.Web), the middleware + OWIN-aware resolver are sufficient.
* Correlation (if enabled) will flow through both pipelines; logs will carry `CorrelationId`.
* See also:
    * [HostBridge.AspNet](../HostBridge.AspNet/README.md) – request scope module & `[FromServices]`
    * [HostBridge.WebApi2](../HostBridge.WebApi2/README.md) – classic resolver (when OWIN isn’t present)