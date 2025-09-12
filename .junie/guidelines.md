# HostBridge – Pairing Contract for Junie

Audience: **Junie agent** (pair programmer / maintainer).
Purpose: enforce repo-specific invariants, avoid drift, and keep examples/tests wired consistently.

---

## Build & Config Invariants

* ✅ Builds must succeed on **Windows** with:

    * .NET Framework dev packs 4.7.2+
    * .NET 8 SDK (latest LTS is acceptable)
* ✅ All `src/*` projects:

    * `LangVersion=preview` (or explicit >= 12)
    * `<Nullable>enable</Nullable>`
    * Must build warning-free (`TreatWarningsAsErrors=true`)
* ✅ `tests/*` and `examples/*`: warnings tolerated, but keep them minimal.
* ✅ Root MSBuild props live in `eng/Directory.Build.props`; do not re-specify in child csprojs unless conditional.

---

## Lifetimes & Scopes

* **Singletons**: one per app domain (`AddSingleton`).
* **Scoped**: one per request/operation (ASP.NET `HttpModule`, WCF `InstanceProvider`, OWIN middleware).
* **Transients**: new per resolve, disposed with owning scope.
* 🚫 Do not resolve from `RootServices` inside request/operation pipelines. Always resolve from the current scope (
  `AspNetRequest.RequestServices`, OWIN env, WCF InstanceContext).
* 🚫 Do not introduce new static state. `HB` accessor exists for console/WinSvc edges only.

---

## Correlation

* Opt-in by config: `HostBridge:Correlation:Enabled=true`.
* Default header: `X-Correlation-Id`.
* `[DisableCorrelation]` attribute must always override config (per contract/service).
* ASP.NET: correlation module pushes/cleans scope per HTTP request.
* WCF: `CorrelationDispatchInspector` pushes scope per operation; must support both SOAP and HTTP headers.
* Console/WinSvc: begin/dispose correlation per loop/job iteration.

---

## Diagnostics

* Always add `HostBridgeVerifier` checks at **dev/test startup**.

    * `ThrowIfCritical()` in dev/test builds.
    * `Log(logger)` in production.
* Critical checks:

    * `AspNetBootstrapper.Initialize(host)` called.
    * `AspNetRequestScopeModule` registered in `web.config`.
    * MVC/WebApi resolvers set to HostBridge resolvers.
    * `HostBridgeWcf.Initialize(host)` called.
* 🚫 Do not ship example apps without verifier calls in place.

---

## OWIN

* Self-host or Katana:

    * Must use `UseHostBridgeRequestScope()` middleware.
    * Must set `WebApiOwinAwareResolver` for Web API 2.
* 🚫 Do not assume ASP.NET module covers OWIN.
* System.Web + OWIN side-by-side is supported; prefer OWIN scope for Web API.

---

## Testing

* Test stack: xUnit + BDDfy + FluentAssertions + Shouldly.
* Parallelization:

    * Disabled in ASP.NET test projects (static `HttpContext.Current` makes tests non-isolated).
    * Allowed elsewhere.
* Naming: `Given_/When_/Then_` style for scenario tests.
* Shared fakes/helpers: `tests/HostBridge.Tests.Common`.
* 🚫 Do not duplicate fakes across projects.
* Must include concurrency tests proving **no scope bleed** across requests/operations.

---

## Examples

* `examples/*` must demonstrate correct wiring:

    * ASP.NET: Global.asax calls `Initialize(host)`; modules/resolvers registered in config.
    * MVC5/WebApi2: resolvers wired.
    * WCF: `.svc` files or config reference `DiServiceHostFactory`.
    * Windows Service: derives from `HostBridgeServiceBase`; graceful stop tested.
    * Console: uses `HB.BeginScope()` and correlation per loop/job.
* 🚫 Do not leave examples half-wired; they must be runnable demos and sanity checks.

---

## Ops/Contrib Hygiene

* Repo must remain **warning-clean in src**.
* XML docs required in src; use `<inheritdoc/>` where possible.
* Each package publishes independently; do not leak cross-package internals.
* PRs must update docs/READMEs if wiring changes.

---

### TL;DR for Junie

* Build on Windows with .NET 8 + 4.8 dev packs.
* `src` = strict (nullable, no warnings).
* `tests/examples` = relaxed but wired correctly.
* Respect lifetimes, scopes, correlation opt-in, and diagnostics checks.
* Wire examples completely; don’t leave stubs.
* Diagnostics must be used in dev/test.
* Never enable ASP.NET test parallelization.

---

Love the pairing contract. I’d add a short **Agent Addendum**—tight rules Junie should actively enforce while
pairing/maintaining. Paste this at the end of the doc.

---

## Agent Addendum (Junie-specific guardrails)

### Constants & naming

* **Single source of truth:** use `HostBridge.Abstractions.Constants` for all keys/headers (request-scope key,
  correlation header). Never inline string literals or redefine per package.
* **Public names:** prefer explicit names over magical defaults; if a name must change, update **all** adapters and docs
  in the same PR.

### API stability & change control

* **No breaking changes** to public APIs in `src/*` without a semver note and README updates.
* If deprecating: add `[Obsolete(..., error:false)]`, ship for 1 release, then remove.
* Keep **TFMs** consistent: netstandard2.0 for shared libs where possible; `net472;net48` for adapter libs.

### Adapters (what Junie must never do)

* **ASP.NET/WCF usage only in adapters.** Do not reference `System.Web.*` or `System.ServiceModel.*` from shared libs.
* **OWIN env access pattern:** read the dictionary via `HttpContext.Current.Items["owin.Environment"]`; retrieve scope
  via `Constants.ScopeKey`. No `new OwinContext(...)`.
* **Never resolve from root** in request/operation code paths—always resolve from the current scope.

### Diagnostics discipline

* **Dev/Test must fail fast:** add `HostBridgeVerifier().ThrowIfCritical()` at startup.
* **Prod logs only:** `verifier.Log(logger)` in production code paths.
* Extend diagnostics with repo-specific checks but **don’t** bake in business config; verifier is for wiring only. (
  Correlation is governed by `CorrelationOptions`.)

### Correlation rules

* **Opt-in only** (config enables it); `[DisableCorrelation]` always wins per contract/service.
* **Propagation:** when adding HTTP clients, attach a delegating handler that stamps `Constants.CorrelationHeaderName`
  if not present.
* **Scopes:** start/stop correlation **at the same lifecycle boundary** as the request/operation/job loop.

### Testing matrix (minimums)

* **Concurrency isolation:** prove no scoped instance bleeds across requests/operations.
* **Disposal determinism:** scoped/transient `IDisposable` are disposed exactly once per request/operation.
* **Adapters under load:** async controller/actions, long-running handlers, faulted WCF calls.
* **ASP.NET test parallelization remains OFF.** Do not enable without per-test host isolation.&#x20;

### Examples: wiring checklist (ship-ready)

* **Global.asax/Startup** uses `LegacyHostBuilder`.
* **ASP.NET**: `AspNetBootstrapper.Initialize(host)` + `AspNetRequestScopeModule` + (optional) correlation module in
  `web.config`.
* **MVC5/WebApi2**: set HostBridge resolvers in code.
* **WCF**: `.svc` or config uses `DiServiceHostFactory`; `HostBridgeWcf.Initialize(host)` called.
* **OWIN**: `UseHostBridgeRequestScope()` + `WebApiOwinAwareResolver`; keep System.Web modules for mixed sites.
* **Windows Service**: base class, graceful stop, optional console mode.

### Repo hygiene & docs

* Update package READMEs when wiring changes; keep **API Surface / Wiring / How it works / Diagnostics / Notes**
  sections in sync.
* Use the example **README template**; every example must be runnable and show DI lifetimes & diagnostics.
* **No duplicate fakes**—put them in `tests/HostBridge.Tests.Common`.

### Build & versions

* Respect `eng/Directory.Build.props` for LangVersion/Nullable/warnings-as-errors; do not override per-project unless
  conditional.
* Pin OWIN/Web API versions; choose a consistent `Microsoft.Extensions.*` line (5.x for Framework is safer; 9.x OK if
  redirects are handled).

### Commit/PR etiquette (for Junie)

* PR includes: code, tests, docs updates, and a one-paragraph **Why** (what wiring or behavior the change enforces).
* If a change touches lifetimes/correlation/diagnostics, include a short **Before/After** in the PR.
