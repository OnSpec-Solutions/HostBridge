[//]: # (./examples/Mvc5/README.md)

# MVC5 Example

💡 *“Controllers that finally resolve from DI.”*

This project demonstrates MVC5 controllers resolving from HostBridge DI.

---

## Wiring

**Global.asax.cs**

```csharp
protected void Application_Start(object sender, EventArgs e)
{
    var host = new LegacyHostBuilder()
        .ConfigureLogging(lb => lb.AddConsole())
        .ConfigureServices((ctx, services) =>
        {
            services.AddOptions();
            services.AddScoped<IMyScoped, MyScoped>();
            services.AddTransient<HomeController>();
        })
        .Build();
    
    AspNetBootstrapper.Initialize(host);
    DependencyResolver.SetResolver(new MvcDependencyResolver());

    AreaRegistration.RegisterAllAreas();
    RouteConfig.RegisterRoutes(RouteTable.Routes);
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

* `MvcDependencyResolver` uses the request scope from the ASP.NET module.
* **Scoped** services are unique per HTTP request.
* **Singletons** live for the app domain.
* **Transients** are per resolve, disposed with the scope.