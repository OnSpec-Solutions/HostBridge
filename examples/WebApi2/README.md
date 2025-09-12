[//]: # (./examples/WebApi2/README.md)

# Web API 2 Example

💡 *“Controllers that don’t secretly use `new`.”*

This project demonstrates Web API 2 controllers resolving from HostBridge DI.

---

## Wiring

**WebApiConfig.cs**

```csharp
public static class WebApiConfig
{
    public static void Register(HttpConfiguration config)
    {
        var host = new LegacyHostBuilder()
            .ConfigureServices((ctx, services) => { services.AddScoped<IMyScoped, MyScoped>(); })
            .Build();

        AspNetBootstrapper.Initialize(host);

        config.DependencyResolver = new WebApiDependencyResolver();

        new HostBridgeVerifier()
            .Add(AspNetChecks.VerifyAspNet)
            .Log(HB.Get<ILogger<WebApiConfig>>());

        config.MapHttpAttributeRoutes();
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

* Resolver uses the per-request scope created by the ASP.NET module.
* OWIN self-host requires `UseHostBridgeRequestScope()` middleware + `WebApiOwinAwareResolver`.
* Scoped lifetimes are isolated per request; no bleed.