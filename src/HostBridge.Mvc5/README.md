[//]: # (./src/HostBridge.Mvc5/README.md)

# HostBridge.Mvc5

💡 *“Controllers resolved the way you always wished they were.”*

This adapter plugs HostBridge DI into **ASP.NET MVC 5** via `DependencyResolver`.  
Controllers, filters, and binders all resolve from your container, with correct lifetimes.

---

## API Surface

- `MvcDependencyResolver` – plugs into MVC5’s `DependencyResolver`

---

## Wiring

**Global.asax.cs**

```csharp
protected void Application_Start()
{
    var host = new LegacyHostBuilder()
        .ConfigureServices((ctx, services) =>
        {
            services.AddScoped<IMyScoped, MyScoped>();
            services.AddSingleton<IMySingleton, MySingleton>();
        })
        .Build();

    AspNetBootstrapper.Initialize(host);

    DependencyResolver.SetResolver(new MvcDependencyResolver());

    new HostBridgeVerifier()
        .Add(AspNetChecks.VerifyAspNet)
        .ThrowIfCritical(); // dev/test
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

* `MvcDependencyResolver` resolves via `AspNetRequest.RequestServices` (the per-request scope).
* **Scoped** services are unique per HTTP request.
* **Singleton** services live for the app domain.
* **Transient** services are created per resolve and disposed with the request scope.

---

## Diagnostics Tip

Wire `HostBridgeVerifier` at startup to catch:

* Missing `AspNetBootstrapper.Initialize(host)`
* Missing `HostBridgeRequestScopeModule` in `web.config`
* Wrong resolver in MVC

---

## Notes

* Correlation (if enabled) enriches all `ILogger` scopes with `CorrelationId`.
* Scoped lifetimes are isolated per request; no bleed across concurrent requests.
* See also:
    * [HostBridge.AspNet](../HostBridge.AspNet/README.md) – base module & `[FromServices]` support
    * [HostBridge.WebApi2](../HostBridge.WebApi2/README.md) – Web API 2 resolver