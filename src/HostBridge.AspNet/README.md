# HostBridge.AspNet

💡 *“Scoped services that actually scope in classic ASP.NET.”*

This package gives you:

* `AspNetBootstrapper.Initialize(host)` — register your root DI container
* `AspNetRequestScopeModule` — an `IHttpModule` that creates a per-request `IServiceScope` and disposes it at the end
* `[FromServices]` — decorate a property on a WebForms `Page` and it gets injected automatically

### Why you want it

Without this, everything is a singleton or a hand-rolled static. With this, `AddScoped` behaves *exactly* like in .NET Core. No cross-request bleed, no more hacks.

### Wire-up

**Global.asax.cs**

```csharp
protected void Application_Start()
{
    var services = new ServiceCollection();
    services.AddScoped<IMyScoped, MyScoped>();
    services.AddSingleton<IMySingleton, MySingleton>();
    var sp = services.BuildServiceProvider();

    var host = new LegacyHost(sp);
    AspNetBootstrapper.Initialize(host);
}
```

**web.config**

```xml
<system.webServer>
  <modules>
    <add name="HostBridgeRequestScope"
         type="HostBridge.AspNet.AspNetRequestScopeModule" />
  </modules>
</system.webServer>
```

**Optional WebForms injection**

```csharp
public partial class Default : Page
{
    [FromServices] public IMyScoped? MyScoped { get; set; }

    protected void Page_Load(object sender, EventArgs e)
    {
        Response.Write(MyScoped?.ToString());
    }
}
```

### Notes

* Scoped services are unique per request.
* Singletons are shared across all requests.
* Forget to call `Initialize(host)`? You’ll get an `InvalidOperationException` early instead of silent weirdness.
