[//]: # (./README.md)

[![License: MIT](https://img.shields.io/badge/License-MIT-green.svg)](LICENSE)
[![Security Policy](https://img.shields.io/badge/Security-Policy-blue.svg)](SECURITY.md)
[![Code of Conduct](https://img.shields.io/badge/Code%20of%20Conduct-Contributor%20Covenant-ff69b4.svg)](CODE_OF_CONDUCT.md)
[![Funding](https://img.shields.io/badge/Funding-Donate-orange.svg)](#funding)
[![Build](https://github.com/OnSpec-Solutions/HostBridge/actions/workflows/ci.yml/badge.svg)](https://github.com/OnSpec-Solutions/HostBridge/actions/workflows/ci.yml)
<!-- Optional (uncomment when enabled)
[![Sponsor](https://img.shields.io/badge/sponsor-❤-pink)](https://github.com/sponsors/yourusername)
[![OpenSSF Scorecard](https://api.securityscorecards.dev/projects/github.com/OnSpec-Solutions/HostBridge/badge)](https://securityscorecards.dev/viewer/?uri=github.com/OnSpec-Solutions/HostBridge)
-->

# HostBridge

💡 *“Sanity for classic .NET developers in an insane world.”*

You’ve got a mountain of ASP.NET WebForms, MVC5, Web API 2, WCF, or Windows Services code that you can’t rewrite to .NET 8 tomorrow. But you **still want DI, Options, Logging, and sane lifetimes** without rolling your own half-broken container glue.

That’s what **HostBridge** is for: drop-in shims that bring modern `Microsoft.Extensions.*` patterns to classic .NET Framework apps.

---

## Contents

- [HostBridge](#hostbridge)
  - [Contents](#contents)
  - [Why this exists](#why-this-exists)
  - [Packages](#packages)
  - [Examples](#examples)
  - [Quick start (ASP.NET)](#quick-start-aspnet)
  - [OWIN (Katana) support](#owin-katana-support)
  - [Quick start (WCF)](#quick-start-wcf)
    - [Console / WinSvc](#console--winsvc)
  - [Correlation Id (trace everything)](#correlation-id-trace-everything)
  - [Diagnostics (fail fast or just warn)](#diagnostics-fail-fast-or-just-warn)
  - [Request lifecycle (no explicit EndRequest)](#request-lifecycle-no-explicit-endrequest)
  - [OWIN + System.Web coexistence (who owns the scope?)](#owin--systemweb-coexistence-who-owns-the-scope)
  - [Shutdown (StopAsync)](#shutdown-stopasync)
  - [Build / Targets](#build--targets)
  - [CI (GitHub Actions): build, test, pack, publish](#ci-github-actions-build-test-pack-publish)
  - [Contributing](#contributing)
  - [Support \& Consulting](#support--consulting)
  - [Funding](#funding)

---

## Why this exists

Because you’re busy. Because “just rewrite it” is not an option. Because you’ve already fought the dragons of `HttpContext.Current` and `OperationContext.Current` more times than you can count.

With HostBridge you can:

* Use `AddScoped`, `AddTransient`, `AddSingleton` **and actually get correct lifetimes** in ASP.NET, Web API 2, MVC5, WCF, Windows Services, and console apps.
* Read `app.config` / `web.config` into `IConfiguration` and bind to options.
* Run background `IHostedService`s in services that predate `GenericHost`.
* Stay sane in legacy land while still writing modern, testable code.

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

> Install via NuGet:
>
> ```bash
> dotnet add package HostBridge.Abstractions
> dotnet add package HostBridge.Core
> dotnet add package HostBridge.Options.Config
> dotnet add package HostBridge.AspNet
> dotnet add package HostBridge.Mvc5
> dotnet add package HostBridge.WebApi2
> dotnet add package HostBridge.Wcf
> dotnet add package HostBridge.WindowsService
> dotnet add package HostBridge.Health
> ```

---

## Examples

We don’t leave you hanging. Check the `examples/` folder for working projects:

* ✅ WebForms (property injection with `[FromServices]`)
* ✅ MVC5 (controllers resolved from DI)
* ✅ Web API 2 (controllers resolved from DI)
* ✅ WCF Service (per-call scoped deps)
* ✅ Windows Service (graceful shutdowns)
* ✅ Console & Composite apps (ambient scopes, hosted services)

---

## Quick start (ASP.NET)

```csharp
using System.Web.Mvc;
using System.Web.Http;
using HostBridge;
using HostBridge.AspNet;
using HostBridge.Options.Config;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

// Global.asax.cs
protected void Application_Start()
{
    var host = new LegacyHostBuilder()
            .ConfigureAppConfiguration(cfg => cfg.AddHostBridgeAppConfig())
            .ConfigureLogging(lb => lb.AddConsole())
            .ConfigureServices((_, services) =>
            {
                services.AddOptions();
                services.AddHostedService<HeartbeatService>();
                services.AddScoped<IMyScoped, MyScoped>();
                services.AddSingleton<IMySingleton, MySingleton>();
            })
            .Build();
    
    AspNetBootstrapper.Initialize(host);

    DependencyResolver.SetResolver(new MvcDependencyResolver());
    GlobalConfiguration.Configuration.DependencyResolver = new WebApiDependencyResolver();
}
```

---

## OWIN (Katana) support

```csharp
public void Configuration(IAppBuilder app)
{
  var host = new LegacyHostBuilder()
            .ConfigureAppConfiguration(cfg => cfg.AddHostBridgeAppConfig())
            .ConfigureLogging(lb => lb.AddConsole())
            .ConfigureServices((_, services) =>
            {
                services.AddOptions();
                services.AddHostedService<HeartbeatService>();
                services.AddScoped<IMyScoped, MyScoped>();
                services.AddSingleton<IMySingleton, MySingleton>();
            })
            .Build();
  
  AspNetBootstrapper.Initialize(host);

  app.UseHostBridgeRequestScope();

  var cfg = new HttpConfiguration();
  cfg.DependencyResolver = new WebApiOwinAwareResolver(); // prefers OWIN scope
  cfg.MapHttpAttributeRoutes();
  app.UseWebApi(cfg);
}
```

> `WebApiOwinAwareResolver` lives in **HostBridge.WebApi2**.

---

## Quick start (WCF)

```csharp
// Global.asax.cs
protected void Application_Start()
{
    var host = new LegacyHostBuilder().Build();
    HostBridge.Wcf.HostBridgeWcf.Initialize(host);
}
```

```aspx
<%@ ServiceHost Language="C#" Debug="true"
    Service="MyNs.MyService"
    Factory="HostBridge.Wcf.DiServiceHostFactory" %>
```

---

### Console / WinSvc

```csharp
using (Correlation.Begin(_log)) { /* logs carry the same CorrelationId */ }
```

---

## Correlation Id (trace everything)

```csharp
services.AddSingleton<ICorrelationAccessor, CorrelationAccessor>();

using (Correlation.Begin(logger)) {
  // all logs include CorrelationId
}
```

---

## Diagnostics (fail fast or just warn)

```csharp
var verifier = new HostBridgeVerifier()
  .Add(AspNetChecks.VerifyAspNet)
  .Add(WcfChecks.VerifyWcf);

// Dev/test:
verifier.ThrowIfCritical();

// Prod:
verifier.Log(logger);
```

---

## Request lifecycle (no explicit EndRequest)

You do not need to manually create/clear request scopes or correlation. HostBridge adapters handle it:

* ASP.NET: modules create/dispose scopes per request.
* OWIN: `UseHostBridgeRequestScope()` does it for you.

---

## OWIN + System.Web coexistence (who owns the scope?)

* Use both System.Web modules + OWIN middleware.
* Web API 2 uses `WebApiOwinAwareResolver` → OWIN scope.
* MVC/WebForms continue using System.Web scope.
* Correlation: enable both correlation adapters.
* Never resolve from the root; always use the current scope.

---

## Shutdown (StopAsync)

Call `StopAsync` at shutdown to flush hosted services and logs.

* **ASP.NET**: `Application_End` in Global.asax.
* **OWIN**: hook `host.OnAppDisposing`.
* **WCF**: same as ASP.NET.
* **Windows Service**: `OnStop` (already in `HostBridgeServiceBase`).
* **Console apps**: call `StopAsync` before exit.

---

## Build / Targets

* Targets: **netstandard2.0**, **net472**, **net48**.
* No RID needed for Framework.
* XML docs ship.
* Warnings as errors.
* Deterministic builds with CI metadata.

---

## CI (GitHub Actions): build, test, pack, publish

* Workflow: `.github/workflows/ci.yml`
* Runs on Windows
* Builds `src/*` in Release, runs tests in Debug
* Uses Nerdbank.GitVersioning (version.json) – prerelease versions on branches/PRs; stable on tags matching `vMAJOR.MINOR.PATCH`
* Packs NuGet on tag push (`v*`) → publishes to NuGet.org
* Uploads `.nupkg` and `.snupkg` as workflow artifacts on PRs and pushes
* Needs `NUGET_API_KEY` secret configured

---

## Contributing

See [CONTRIBUTING.md](CONTRIBUTING.md).

---

## Support & Consulting

MIT and free. If it saved you headaches, consider funding.
Contact: [community@onspec.solutions](mailto:community@onspec.solutions)

---

## Funding

💡 **HostBridge is free forever — no paywalls, no feature gates.**

If it saved you a late night, helped squash a `NullReferenceException`, or just kept your legacy app sane, you can:

<!--
- ⭐ [Sponsor on GitHub](https://github.com/sponsors/jeffrey-onspec) (recurring support)
-->
- ☕ [Buy a coffee on Ko-fi](https://ko-fi.com/yourhandle) (one-time thank-you)
- (Future) Sponsor on GitHub (recurring support)
- (Future) Open Collective for transparent budgets once we hit traction

**Bucket is full rule:** when current goals are met, we pause donations until the next plan is posted.
