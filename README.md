[//]: # (./README.md)

[![License: MIT](https://img.shields.io/badge/License-MIT-green.svg)](LICENSE)
[![Security Policy](https://img.shields.io/badge/Security-Policy-blue.svg)](SECURITY.md)
[![Code of Conduct](https://img.shields.io/badge/Code%20of%20Conduct-Contributor%20Covenant-ff69b4.svg)](CODE_OF_CONDUCT.md)
[![Funding](https://img.shields.io/badge/Funding-Donate-orange.svg)](#funding)
[![Sponsor](https://img.shields.io/badge/sponsor-❤-pink)](https://github.com/sponsors/yourusername)
<!-- Optional (uncomment when enabled)
[![Build](https://github.com/REPO_OWNER/REPO_NAME/actions/workflows/dotnet.yml/badge.svg)](../../actions/workflows/dotnet.yml)
[![OpenSSF Scorecard](https://api.securityscorecards.dev/projects/github.com/REPO_OWNER/REPO_NAME/badge)](https://securityscorecards.dev/viewer/?uri=github.com/REPO_OWNER/REPO_NAME)
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
  - [Build / Targets](#build--targets)
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
>
> <!-- Optional: add per-package shields once published
> [![NuGet Abstractions](https://img.shields.io/nuget/v/HostBridge.Abstractions.svg)](https://www.nuget.org/packages/HostBridge.Abstractions)
> [![NuGet Core](https://img.shields.io/nuget/v/HostBridge.Core.svg)](https://www.nuget.org/packages/HostBridge.Core)
> ... -->

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

Register a small HTTP module that begins/ends a correlation per HTTP request (reads `X-Correlation-Id` if present):

```xml
<system.webServer>
  <modules>
    <!-- your request-scope module -->
    <add name="HostBridgeRequestScope" type="HostBridge.AspNet.AspNetRequestScopeModule" />
    <!-- (optional) correlation scope module -->
    <add name="HostBridgeCorrelation"  type="HostBridge.AspNet.CorrelationHttpModule" />
  </modules>
</system.webServer>
```

Now `AddScoped` really means “per request.” No more static-bleed.

---

## OWIN (Katana) support

If you’re hosting **Web API 2 in OWIN** (with or without System.Web), add a single middleware that creates/disposes an `IServiceScope` per request. Use an OWIN-aware resolver that prefers the OWIN scope (falls back to System.Web scope if present).

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
  
  AspNetBootstrapper.Initialize(host); // or your own static root

  app.UseHostBridgeRequestScope(); // OWIN scope per request

  var cfg = new HttpConfiguration();
  cfg.DependencyResolver = new WebApiOwinAwareResolver(); // prefers OWIN scope
  cfg.MapHttpAttributeRoutes();
  app.UseWebApi(cfg);
}
```

> `WebApiOwinAwareResolver` lives in **HostBridge.WebApi2**.

Result: `AddScoped` means **per OWIN request**; disposal happens at the end of the pipeline.

---

## Quick start (WCF)

```csharp
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
    HostBridge.Wcf.HostBridgeWcf.Initialize(host);
}
```

```aspx
<%@ ServiceHost Language="C#" Debug="true"
    Service="MyNs.MyService"
    Factory="HostBridge.Wcf.DiServiceHostFactory" %>
```

Your service gets proper scoped deps per call. They get disposed when the call ends. Sanity restored.

---

### Console / WinSvc

Begin a correlation per loop iteration, job, or message:

```csharp
using (Correlation.Begin(_log)) { /* logs carry the same CorrelationId */ }
```

---

## Correlation Id (trace everything)

Add a tiny ambient correlation layer so every request/operation logs with the same `CorrelationId`, and outbound calls propagate it.

```csharp
// Startup (any host)
services.AddSingleton<ICorrelationAccessor, CorrelationAccessor>();

// Start a correlation for the current flow (auto-generates if none supplied)
using (Correlation.Begin(logger)) {
  // do work; all logs include CorrelationId
}
```

> The correlation module reads/writes `X-Correlation-Id`. Forward it on outbound HTTP calls so downstream logs stitch correctly.

---

## Diagnostics (fail fast or just warn)

Add `HostBridge.Diagnostics` and call it at startup. It screams—in plain English—when wiring is missing (e.g., ASP.NET module not registered, MVC/Web API resolver not set, WCF factory missing).

```csharp
var verifier = new HostBridgeVerifier()
  .Add(AspNetChecks.VerifyAspNet)
  .Add(WcfChecks.VerifyWcf);

// Dev/test: throw on misconfig
// verifier.ThrowIfCritical();

// Prod: log and keep going
verifier.Log(logger);
```

* ASP.NET checks: `Initialize(host)` called? scope module present? resolvers set?
* WCF checks: `HostBridgeWcf.Initialize(host)` called? service factory hint?
* Windows Service: console vs. SCM notes

> **Tip:** Enable `ThrowIfCritical()` only in dev/test. In production, prefer `Log(logger)` so misconfigurations are visible without crashing the app.

---

## Build / Targets

* Libraries target **netstandard2.0** (where possible) + **net472/net48**.
* Examples are classic Framework projects (`packages.config`).
* XML docs ship in packages.
* Treat warnings as errors in `src/`.
* Deterministic builds: `ContinuousIntegrationBuild=true`, `RepositoryUrl`, `RepositoryCommit` embedded in packages.

---

## Contributing

PRs welcome. Tests use xUnit + BDDfy + FluentAssertions + Shouldly.
Run the full suite before submitting.

See [CONTRIBUTING.md](CONTRIBUTING.md) for details.
License: MIT. Use it, fork it, ship it — and hopefully sleep better at night.

---

## Support & Consulting

HostBridge is MIT and free. If it’s saved you hours of debugging or a production outage,
please consider supporting its development (see [Funding](#funding)).

Short targeted help is available (audits, DI lifetimes, migration strategy).
**Contact:** [community@onspec.solutions](mailto:community@onspec.solutions)

---

## Funding

HostBridge is donation-ware: free forever, no paywall. If it helps you ship, please consider supporting ongoing maintenance and improvements.

* **GitHub Sponsor button** on this repo (if enabled)
* **Open Collective** (transparent budget): **\[Add your link]**
* **Other links** (Stripe/Ko-fi/Patreon): see the Sponsor button or `.github/FUNDING.yml`

**Bucket is full rule.** We accept funds only up to the current published goal. When the goal is met, we pause additional intake until the next plan/goal is posted. See [FUNDING.md](FUNDING.md) for details.
