[//]: # (./src/HostBridge.AspNet/README.md)

# HostBridge.AspNet

💡 *“Scoped services that actually scope in classic ASP.NET.”*

This package establishes per-request DI scopes in **ASP.NET (System.Web)** apps.

---

## API Surface

- `AspNetBootstrapper.Initialize(host)` – registers the root `ILegacyHost`
- `AspNetRequestScopeModule` – `IHttpModule` that creates/disposes a per-request scope
- `[FromServices]` – attribute for property injection in WebForms pages
- `AspNetRequest.RequestServices` – static accessor for the current request scope

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

    DependencyResolver.SetResolver(new MvcDependencyResolver()); // if MVC present

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

**WebForms injection**

```csharp
public partial class Default : Page
{
    [FromServices] public IMyScoped? Scoped { get; set; }
    protected void Page_Load(object sender, EventArgs e)
    {
        Response.Write(Scoped?.ToString());
    }
}
```

---

## How it works

* `AspNetRequestScopeModule` creates an `IServiceScope` at `BeginRequest`, disposes at `EndRequest`.
* MVC5 and Web API 2 resolvers delegate to this scope.
* WebForms property injection works via `[FromServices]`.
* Scoped services are unique per HTTP request; singletons live for the app domain.

---

## Diagnostics Tip

Verifier catches:

* Missing bootstrap/init
* Missing module in `web.config`
* Resolver not set in MVC/Web API

---

## Notes

* Correlation is opt-in via config + `CorrelationHttpModule`.
* Scoped lifetimes are isolated; no bleed across concurrent requests.
* See also:
    * [HostBridge.Mvc5](../HostBridge.Mvc5/README.md)
    * [HostBridge.WebApi2](../HostBridge.WebApi2/README.md)