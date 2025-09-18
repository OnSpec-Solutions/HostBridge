[//]: # (./examples/WebForms/README.md)

# WebForms Example

💡 *“Even your .aspx pages deserve scoped DI.”*

This project demonstrates **ASP.NET WebForms** pages using HostBridge for property injection
via `[FromServices]`.

---

## API Surface

- `[FromServices]` – decorate page properties to have them injected
- `AspNetRequestScopeModule` – creates a per-request scope
- `AspNetBootstrapper.Initialize(host)` – registers the root host

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

**Default.aspx.cs**

```csharp
public partial class _Default : Page
{
    [FromServices] public IMyScoped? Scoped { get; set; }

    protected void Page_Load(object sender, EventArgs e)
    {
        Response.Write($"Scoped.Id = {Scoped?.Id}");
    }
}
```

---

## How it works

* Each HTTP request gets its own `IServiceScope`.
* `[FromServices]` properties on `Page` objects are filled from the scope.
* Scoped services are unique per request; singletons are global.

---

## Diagnostics Tip

Use `HostBridgeVerifier().Add(AspNetChecks.VerifyAspNet)` at startup to catch misconfigurations.

---

## Notes

* Correlation is opt-in via `web.config` appSettings (`HostBridge:Correlation:Enabled=true`).
* MVC/Web API can run alongside WebForms and share the same request scope.