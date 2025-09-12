[//]: # (./examples/WcfService/README.md)

# WCF Example

💡 *“Per-call DI in WCF, without tears.”*

This project demonstrates WCF services resolving via HostBridge DI.

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
        })
        .Build();

    HostBridgeWcf.Initialize(host);

    new HostBridgeVerifier()
        .Add(WcfChecks.VerifyWcf)
        .ThrowIfCritical();
}
```

**Service1.svc**

```aspx
<%@ ServiceHost Language="C#" Debug="true"
    Service="WcfService.MyService"
    Factory="HostBridge.Wcf.DiServiceHostFactory" %>
```

**web.config**

```xml
<appSettings>
  <add key="HostBridge:Correlation:Enabled" value="true" />
  <add key="HostBridge:Correlation:HeaderName" value="X-Correlation-Id" />
</appSettings>
```

---

## How it works

* New scope per operation call.
* Scoped deps unique per call; disposed at end.
* Correlation opt-in via config, opt-out with `[DisableCorrelation]`.