[//]: # (./examples/OwinComposite/README.md)

# OWIN Composite Example

💡 *“System.Web + OWIN living together, with DI that actually works.”*

This project demonstrates **OWIN + System.Web** running side-by-side,
with Web API 2 controllers resolving from the correct scope.

---

## API Surface

- `UseHostBridgeRequestScope()` – OWIN middleware creating per-request scopes
- `WebApiOwinAwareResolver` – resolves via OWIN env scope, fallback to System.Web scope
- `AspNetBootstrapper.Initialize(host)` – sets up root services

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

        AspNetBootstrapper.Initialize(host);

        // OWIN branch: per-request scope for Web API pipeline
        app.UseHostBridgeRequestScope();

        var config = new HttpConfiguration();
        config.DependencyResolver = new HostBridge.Owin.WebApiOwinAwareResolver();
        config.MapHttpAttributeRoutes();
        app.UseWebApi(config);

        new HostBridgeVerifier()
            .Add(AspNetChecks.VerifyAspNet)
            .ThrowIfCritical();
    }
}
```

**web.config**

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

* System.Web requests (WebForms, MVC) use `AspNetRequestScopeModule`.
* OWIN requests (Web API 2 pipeline) use `UseHostBridgeRequestScope()` middleware.
* `WebApiOwinAwareResolver` prefers the OWIN scope, falls back to System.Web if present.
* Scoped services are unique per request, regardless of pipeline.

---

## Diagnostics Tip

Verifier catches:

* Missing bootstrap/init
* Missing modules in `web.config`
* Wrong resolver wiring

---

## Notes

* Correlation (if enabled) flows through both System.Web and OWIN scopes.
* Useful for hybrid apps migrating from System.Web to OWIN self-host.