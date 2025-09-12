[//]: # (./examples/Mvc5/README.md)

# MVC5 Example

💡 *“Controllers that finally resolve from DI.”*

This project demonstrates MVC5 controllers resolving from HostBridge DI.

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
        })
        .Build();

    AspNetBootstrapper.Initialize(host);

    DependencyResolver.SetResolver(new MvcDependencyResolver());

    new HostBridgeVerifier()
        .Add(AspNetChecks.VerifyAspNet)
        .ThrowIfCritical();
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