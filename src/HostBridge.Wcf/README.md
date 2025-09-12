[//]: # (./src/HostBridge.Wcf/README.md)

# HostBridge.Wcf

💡 *“Per-call DI in WCF, without tears.”*

This package integrates HostBridge DI into **Windows Communication Foundation (WCF)**.  
Each operation call gets its own `IServiceScope`.

---

## API Surface

- `HostBridgeWcf.Initialize(host)` – set root services at app start
- `DiServiceHostFactory` – plug into `.svc` or config
- `DiServiceHost` – per-contract DI wiring
- `DiInstanceProvider` – resolves services per call
- `CorrelationBehavior` + `CorrelationDispatchInspector` – optional correlation wiring (opt-in by config, opt-out by `[DisableCorrelation]`)

---

## Wiring

**Global.asax.cs**

```csharp
protected void Application_Start()
{
    var host = new LegacyHostBuilder()
        .ConfigureServices((ctx, services) =>
        {
            services.AddTransient<MyService>();
            services.AddScoped<IMyScopedDep, MyScopedDep>();
            services.AddSingleton<ISingletonDep, SingletonDep>();
        })
        .Build();

    HostBridgeWcf.Initialize(host);

    new HostBridgeVerifier()
        .Add(WcfChecks.VerifyWcf)
        .ThrowIfCritical();
}
````

**Service1.svc**

```aspx
<%@ ServiceHost Language="C#" Debug="true"
    Service="MyNs.MyService"
    Factory="HostBridge.Wcf.DiServiceHostFactory" %>
```

**Optional web.config (correlation)**

```xml
<appSettings>
  <add key="HostBridge:Correlation:Enabled" value="true" />
  <add key="HostBridge:Correlation:HeaderName" value="X-Correlation-Id" />
</appSettings>
```

---

## How it works

* `DiInstanceProvider` creates a new scope per call.
* Scoped dependencies are unique per call and disposed at release.
* `[DisableCorrelation]` attribute disables correlation for specific services/contracts.
* Supports both SOAP headers and HTTP headers for correlation ID.

---

## Diagnostics Tip

Verifier checks:

* `HostBridgeWcf.Initialize(host)` was called
* Service factory is HostBridge’s (`DiServiceHostFactory`)

---

## Notes

* Correlation is opt-in via config.
* Scoped lifetimes are guaranteed per operation; no bleed across calls.
* See also:
    * [HostBridge.Diagnostics](../HostBridge.Diagnostics/README.md) – wiring checks
    * [HostBridge.Core](../HostBridge.Core/README.md) – ambient accessor