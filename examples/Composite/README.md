[//]: # (./examples/Composite/README.md)

# Composite Example

💡 *“WebForms + Web API 2 + WCF in one app domain (the real-world nightmare).”*

This project demonstrates multiple classic stacks coexisting in one IIS site.

---

## Wiring

**Global.asax.cs**

```csharp
using System.Web.Http;
using System.Web.Mvc;
using HostBridge;
using HostBridge.AspNet;
using HostBridge.Wcf;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

public class Global : HttpApplication
{
    protected void Application_Start(object sender, EventArgs e)
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

        GlobalConfiguration.Configure(WebApiConfig.Register);
        GlobalConfiguration.Configuration.DependencyResolver = new WebApiDependencyResolver();
        GlobalConfiguration.Configuration.Services.Replace(
            typeof(IHttpControllerActivator),
            new HostBridgeControllerActivator());
    }

#if DEBUG
    private static volatile bool s_verified;
#endif
    protected void Application_BeginRequest()
    {
#if DEBUG
        if (s_verified) return;
        // Only runs once per debug session to validate wiring; skipped in Release.
        new HostBridgeVerifier()
            .Add(AspNetChecks.VerifyAspNet)
            .Add(WcfChecks.VerifyWcf)
            .ThrowIfCritical();
        s_verified = true;
#endif
    }
}
```

**Web.config**

```xml
<system.webServer>
  <modules>
    <add name="HostBridgeRequestScope" type="HostBridge.AspNet.AspNetRequestScopeModule" />
    <add name="HostBridgeCorrelation"  type="HostBridge.AspNet.CorrelationHttpModule" />
  </modules>
</system.webServer>

<system.serviceModel>
  <services>
    <service name="Composite.Services.MyWcfService"
             factory="HostBridge.Wcf.DiServiceHostFactory">
      <endpoint address="" binding="basicHttpBinding" contract="Composite.Services.IMyWcfService" />
    </service>
  </services>
</system.serviceModel>
```

---

## How it works

* WebForms injection via `[FromServices]`
* MVC5 + Web API 2 controllers resolve from per-request scope
* WCF services resolve from per-call scope
* Scoped lifetimes are isolated; no bleed across stacks
* Correlation flows via HTTP headers (ASP.NET/Web API) and SOAP/HTTP headers (WCF)

---

## Running the sample

1. Open the solution in Visual Studio.
2. Run under IIS Express (or configure an IIS site).
3. Substitute the port from your run profile in the curl commands below.

### Verify wiring

* **Diagnostics (DEBUG only)**
  Hit any endpoint once; `HostBridgeVerifier` will throw if wiring is misconfigured.

* **Correlation Id (Web API)**

  * Without header (server generates one):

    ```sh
    curl http://localhost:PORT/api/correlation
    ```

    → `{ "Header": "X-Correlation-Id", "Id": "<non-empty>" }`
  * With header (client-supplied):

    ```sh
    curl -H "X-Correlation-Id: demo-123" http://localhost:PORT/api/correlation
    ```

    → `{ "Header": "X-Correlation-Id", "Id": "demo-123" }`

* **Scoped DI isolation**
  Call twice:

  ```sh
  curl http://localhost:PORT/api/scoped
  ```

  → two different Id values between calls (per-request scope).

---

## Notes

* Request scopes end automatically; no manual cleanup needed.
* `AspNetRequestScopeModule` + `CorrelationHttpModule` handle Begin/End.
* For outbound `HttpClient` calls, add `HostBridge.Core.Http.CorrelationDelegatingHandler` to propagate `X-Correlation-Id`.
