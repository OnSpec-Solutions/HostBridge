# HostBridge

💡 *“Sanity for classic .NET developers in an insane world.”*

You’ve got a mountain of ASP.NET WebForms, MVC5, Web API 2, WCF, or Windows Services code that you can’t rewrite to .NET 8 tomorrow. But you **still want DI, Options, Logging, and sane lifetimes** without rolling your own half-broken container glue.

That’s what **HostBridge** is for: drop-in shims that bring `Microsoft.Extensions.*` patterns to the “classic” stack.

---

## Why this exists

Because you’re busy. Because “just rewrite it” is not an option. Because you’ve already fought the dragons of `HttpContext.Current` and `OperationContext.Current` more times than you can count.

With HostBridge you can:

* Use `AddScoped`, `AddTransient`, `AddSingleton` **and actually get correct lifetimes** in ASP.NET, Web API 2, MVC5, WCF, Windows Services, and console apps.
* Read `app.config` / `web.config` into `IConfiguration` and bind to options.
* Run background `IHostedService`s in services that predate `GenericHost`.
* Keep your head above water in legacy land while still writing modern, testable code.

---

## Packages

Each piece is factored so you only pull what you need:

* **HostBridge.Abstractions** – core contracts (`ILegacyHost`, `IHostedService`, `HostContext`)
* **HostBridge.Core** – the `HB` static accessor, ambient scopes, helpers
* **HostBridge.Options.Config** – add `app.config` / `web.config` to `IConfiguration`
* **HostBridge.AspNet** – System.Web bootstrapper + per-request scope `IHttpModule` + `[FromServices]` for WebForms
* **HostBridge.Mvc5** – MVC5 `IDependencyResolver` wired into the request scope
* **HostBridge.WebApi2** – Web API 2 `IDependencyResolver` wired into the request scope
* **HostBridge.Wcf** – `ServiceHostFactory` + instance provider for per-operation DI
* **HostBridge.WindowsService** – base class for DI-driven Windows Services
* **HostBridge.Health** – tiny primitives for health checks

NuGet packages are published individually, each with its own README and wiring snippet.

---

## Examples

We don’t leave you hanging. Check the `examples/` folder for working projects:

* ✅ WebForms (property injection with `[FromServices]`)
* ✅ MVC5 (controllers resolved from DI)
* ✅ Web API 2 (controllers resolved from DI)
* ✅ WCF Service (per-call scoped deps)
* ✅ Windows Service (graceful shutdowns)
* ✅ Console & Composite apps (ambient scopes, hosted services)

Each example is designed to build, run, and show you DI lifetimes in action.

---

## Quick start (ASP.NET)

```csharp
// Global.asax.cs
protected void Application_Start()
{
    var services = new ServiceCollection();
    services.AddOptions();
    services.AddScoped<IMyScoped, MyScoped>();
    services.AddSingleton<IMySingleton, MySingleton>();
    var sp = services.BuildServiceProvider();

    var host = new LegacyHost(sp);
    AspNetBootstrapper.Initialize(host);

    DependencyResolver.SetResolver(new MvcDependencyResolver());
    GlobalConfiguration.Configuration.DependencyResolver = new WebApiDependencyResolver();
}
```

```xml
<!-- web.config -->
<system.webServer>
  <modules>
    <add name="HostBridgeRequestScope" type="HostBridge.AspNet.AspNetRequestScopeModule" />
  </modules>
</system.webServer>
```

Now `AddScoped` really means “per request.” No more static-bleed.

---

## Quick start (WCF)

```csharp
// Global.asax.cs
protected void Application_Start()
{
    var services = new ServiceCollection();
    services.AddScoped<IMyScopedDep, MyScopedDep>();
    var sp = services.BuildServiceProvider();
    HostBridge.Wcf.HostBridgeWcf.Initialize(new LegacyHost(sp));
}
```

```aspx
<%@ ServiceHost Language="C#" Debug="true"
    Service="MyNs.MyService"
    Factory="HostBridge.Wcf.DiServiceHostFactory" %>
```

Your service gets proper scoped deps per call. They get disposed when the call ends. Sanity restored.

---

## Build / Targets

* Libraries target **netstandard2.0** (where possible) + **net472/net48**.
* Examples are classic Framework projects (`packages.config`).
* XML docs ship in packages.
* Treat warnings as errors in `src/`.

---

## Contributing

PRs welcome. Tests use xUnit + BDDfy + FluentAssertions + Shouldly.
Run the full suite before submitting.

License: MIT. Use it, fork it, ship it — and hopefully sleep better at night.
