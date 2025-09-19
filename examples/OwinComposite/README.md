[//]: # (./examples/OwinComposite/README.md)

# OWIN Composite Example

💡 *“System.Web + OWIN living together, with DI that actually works.”*

This project demonstrates **OWIN + System.Web** running side-by-side, with Web API 2 controllers resolving from the correct scope.

---

## API Surface

- `UseHostBridgeRequestScope()` – OWIN middleware creating per-request scopes  
- `WebApiOwinAwareResolver` – resolves via OWIN env scope, fallback to System.Web scope  
- `AspNetBootstrapper.Initialize(host)` – sets up root services  

---

## Wiring

**Startup.cs**

```csharp
using System.Web.Http;
using HostBridge;
using HostBridge.AspNet;
using HostBridge.Wcf;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

public class Startup
{
    public void Configuration(IAppBuilder app)
    {
        var host = new LegacyHostBuilder()
            .ConfigureLogging(lb => lb.AddConsole())
            .ConfigureServices((ctx, services) =>
            {
                services.AddOptions();
                services.AddScoped<IMyScoped, MyScoped>();
            })
            .Build();

        AspNetBootstrapper.Initialize(host);
        HB.Initialize(host);
        HostBridgeWcf.Initialize(host);

        // HostBridge scope + correlation for OWIN pipeline
        app.UseHostBridgeRequestScope();
        app.UseHostBridgeCorrelation();

        var config = new HttpConfiguration();
        config.DependencyResolver = new WebApiOwinAwareResolver();
        WebApiConfig.Register(config);
        app.UseWebApi(config);

#if DEBUG
        // Fail fast in Debug builds if wiring is incomplete
        new HostBridgeVerifier()
            .Add(AspNetChecks.VerifyAspNet)
            .Add(WcfChecks.VerifyWcf)
            .ThrowIfCritical();
#else
        var logger = host.ServiceProvider
            .GetRequiredService<ILoggerFactory>()
            .CreateLogger("Diagnostics");
        new HostBridgeVerifier()
            .Add(AspNetChecks.VerifyAspNet)
            .Add(WcfChecks.VerifyWcf)
            .Log(logger);
#endif
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

* System.Web requests (WebForms, MVC) use `AspNetRequestScopeModule`
* OWIN requests (Web API 2 pipeline) use `UseHostBridgeRequestScope()` middleware
* `WebApiOwinAwareResolver` prefers the OWIN scope, falls back to System.Web if present
* Scoped services are unique per request, regardless of pipeline

---

## Diagnostics

Verifier catches common problems:

* Missing bootstrap/init
* Missing modules in `web.config`
* Wrong resolver wiring

---

## Verify wiring

Run under IIS Express (or an IIS site) and substitute the port from your run profile:

* **Diagnostics (DEBUG only)**
  Hit any endpoint once; `HostBridgeVerifier` will throw if miswired.

* **Correlation Id (Web API via OWIN)**

  * Without header (server generates one):

    ```sh
    curl http://localhost:PORT/api/correlation
    ```

    → `{ "Header": "X-Correlation-Id", "Id": "<non-empty>" }`
  * With header (client-supplied):

    ```sh
    curl -H "X-Correlation-Id: demo-owin" http://localhost:PORT/api/correlation
    ```

    → `{ "Header": "X-Correlation-Id", "Id": "demo-owin" }`

* **Mixed pipeline sanity**

  * Web API 2 (running in OWIN) uses the OWIN request scope
  * System.Web requests (WebForms/MVC) use the System.Web module scope
  * For a combined MVC/WebForms/WCF example, see the [Composite](../Composite) sample

---

## Notes

* Correlation flows through both System.Web and OWIN scopes if enabled
* Useful for hybrid apps migrating from System.Web to OWIN self-host
