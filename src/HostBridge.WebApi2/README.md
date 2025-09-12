[//]: # (./src/HostBridge.WebApi2/README.md)

# HostBridge.WebApi2

💡 *“Web API 2 controllers that don’t secretly use `new`.”*

This adapter plugs HostBridge DI into **ASP.NET Web API 2** via its `IDependencyResolver`.  
Controllers, filters, and message handlers resolve from the per-request scope.

---

## API Surface

- `WebApiDependencyResolver` – resolves from ASP.NET request scope
- `WebApiOwinAwareResolver` – resolves from OWIN scope (preferred in OWIN hosts)

---

## Wiring

**WebApiConfig.cs**

```csharp
public static class WebApiConfig
{
    public static void Register(HttpConfiguration config)
    {
        var host = new LegacyHostBuilder()
            .ConfigureServices((ctx, services) =>
            {
                services.AddScoped<IMyScoped, MyScoped>();
            })
            .Build();

        AspNetBootstrapper.Initialize(host);

        config.DependencyResolver = new WebApiDependencyResolver();

        new HostBridgeVerifier()
            .Add(AspNetChecks.VerifyAspNet)
            .Log(HB.Get<ILogger<WebApiConfig>>()); // prod
    }
}
````

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

* Resolver uses `AspNetRequest.RequestServices` from the request scope module.
* **Scoped** services are unique per HTTP request.
* **Singletons** live for the app domain.
* **Transients** are per resolve and disposed at the end of the request.
* In OWIN self-host: use `UseHostBridgeRequestScope()` middleware + `WebApiOwinAwareResolver`.

---

## Diagnostics Tip

Verifier catches:

* Missing bootstrap/init
* Resolver not set
* Module not registered

---

## Notes

* Correlation (if enabled) flows via `X-Correlation-Id`.
* Supports both System.Web and OWIN hosting.
* See also:
    * [HostBridge.AspNet](../HostBridge.AspNet/README.md) – per-request scope module
    * [HostBridge.Mvc5](../HostBridge.Mvc5/README.md) – MVC resolver