# HostBridge.Wcf

💡 *“Per-call DI in WCF, without crying.”*

This package swaps WCF’s instance provider with one that understands DI scopes.

### Wire-up

**Global.asax.cs**

```csharp
protected void Application_Start()
{
    var services = new ServiceCollection();
    services.AddTransient<MyService>();              // service class transient
    services.AddScoped<IMyScopedDep, MyScopedDep>(); // per-operation
    services.AddSingleton<ISingletonDep, SingletonDep>();

    var sp = services.BuildServiceProvider();
    var host = new LegacyHost(sp);
    HostBridge.Wcf.HostBridgeWcf.Initialize(host);
}
```

**.svc file**

```aspx
<%@ ServiceHost Language="C#" Debug="true"
    Service="MyNs.MyService"
    Factory="HostBridge.Wcf.DiServiceHostFactory" %>
```

**Or web.config**

```xml
<service name="MyNs.MyService"
         factory="HostBridge.Wcf.DiServiceHostFactory">
  <endpoint address=""
            binding="basicHttpBinding"
            contract="MyNs.IMyService" />
</service>
```

**Service class**

```csharp
[ServiceBehavior(InstanceContextMode = InstanceContextMode.PerCall,
                 ConcurrencyMode = ConcurrencyMode.Multiple)]
public class MyService : IMyService
{
    private readonly IMyScopedDep _dep;
    public MyService(IMyScopedDep dep) => _dep = dep;
}
```

### Notes

* A new DI `IServiceScope` is created per operation call.
* Scoped deps are unique per call and disposed at the end.
* Singletons are shared app-wide.
* If `HostBridgeWcf.Initialize(host)` wasn’t called, resolution throws instead of half-working.
