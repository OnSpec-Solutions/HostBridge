[//]: # (./examples/WebApi2/README.md)

# Web API 2 Example

💡 *“Controllers that don’t secretly use `new`.”*

This project demonstrates Web API 2 controllers resolving from HostBridge DI.

---

## Wiring

**Global.asax.cs**

```csharp
protected void Application_Start()
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
    
    GlobalConfiguration.Configure(WebApiConfig.Register);
    GlobalConfiguration.Configuration.DependencyResolver = new WebApiDependencyResolver();
    // Optional: If you prefer to not register controllers one at a time
    GlobalConfiguration.Configuration.Services.Replace(typeof(IHttpControllerActivator), new HostBridgeControllerActivator());
}
        
#if DEBUG        
private static volatile bool s_aspNetVerified;
#endif
        
protected void Application_BeginRequest()
{
#if DEBUG
    if (s_aspNetVerified) return;
    new HostBridgeVerifier()
        .Add(AspNetChecks.VerifyAspNet)
        .ThrowIfCritical();
    s_aspNetVerified = true;
#endif 
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