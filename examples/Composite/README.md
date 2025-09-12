[//]: # (./examples/Composite/README.md)

# Composite Example

💡 *“WebForms + Web API 2 + WCF in one app domain (the real-world nightmare).”*

This project demonstrates multiple classic stacks coexisting in one IIS site.

---

## Wiring

**Global.asax.cs**

```csharp
protected void Application_Start()
{
    var host = new LegacyHostBuilder()
        .ConfigureServices((ctx, services) =>
        {
            services.AddOptions();
            services.AddScoped<IMyScoped, MyScoped>();
        })
        .Build();

    AspNetBootstrapper.Initialize(host);
    HostBridgeWcf.Initialize(host);

    DependencyResolver.SetResolver(new MvcDependencyResolver());
    GlobalConfiguration.Configure(cfg =>
    {
        cfg.DependencyResolver = new WebApiDependencyResolver();
        WebApiConfig.Register(cfg);
    });

    new HostBridgeVerifier()
        .Add(AspNetChecks.VerifyAspNet)
        .Add(WcfChecks.VerifyWcf)
        .ThrowIfCritical();
}
```

**Web.config**

```xml
<system.webServer>
  <modules>
    <add name="HostBridgeRequestScope" type="HostBridge.AspNet.AspNetRequestScopeModule" />
    <add name="HostBridgeCorrelation"  type="HostBridge.AspNet.CorrelationHttpModule" />
  </modules>
</system.webServer>

<system.serviceModel>
  <services>
    <service name="Composite.Services.MyWcfService"
             factory="HostBridge.Wcf.DiServiceHostFactory">
      <endpoint address="" binding="basicHttpBinding" contract="Composite.Services.IMyWcfService" />
    </service>
  </services>
</system.serviceModel>
```

---

## How it works

* WebForms injection via `[FromServices]`.
* MVC5 + Web API 2 controllers resolve from per-request scope.
* WCF services resolve from per-call scope.
* Scoped lifetimes are isolated; no bleed across stacks.
* Correlation flows via HTTP headers (ASP.NET/Web API) and SOAP/HTTP headers (WCF).