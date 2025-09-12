# HostBridge.Mvc5

💡 *“Controllers resolved the way you always wished they were.”*

This adapter plugs DI into MVC5’s `DependencyResolver`. That means controllers, filters, and binders all come from your container.

### Wire-up

```csharp
protected void Application_Start()
{
    var services = new ServiceCollection();
    services.AddScoped<IMyScoped, MyScoped>();
    var sp = services.BuildServiceProvider();

    var host = new LegacyHost(sp);
    AspNetBootstrapper.Initialize(host);

    DependencyResolver.SetResolver(new MvcDependencyResolver());
}
```

### How it works

`MvcDependencyResolver` delegates to `AspNetRequest.RequestServices`, which points to the per-request scope created by the ASP.NET module.

Result: `AddScoped` really is per request; `AddSingleton` is global.

---

# HostBridge.WebApi2

💡 *“Web API 2 controllers that don’t secretly use `new`.”*

This adapter wires DI into Web API 2 via its `IDependencyResolver`.

### Wire-up

```csharp
public static class WebApiConfig
{
    public static void Register(HttpConfiguration config)
    {
        var services = new ServiceCollection();
        services.AddScoped<IMyScoped, MyScoped>();
        var sp = services.BuildServiceProvider();
        AspNetBootstrapper.Initialize(new LegacyHost(sp));

        config.DependencyResolver = new WebApiDependencyResolver();

        config.MapHttpAttributeRoutes();
    }
}
```

### How it works

* `WebApiDependencyResolver` resolves via `AspNetRequest.RequestServices`.
* The ASP.NET module owns the real request scope; Web API just plugs into it.
* Scoped services are unique per HTTP request.
* Singletons are app-wide.
