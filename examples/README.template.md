[//]: # (./examples/README.template.md)

# Example: {ExampleName}

This project demonstrates {tech stack or host type} using HostBridge.

---

## Wiring

**Global.asax / Startup / Program.cs**

```csharp
// Build the host
var host = new LegacyHostBuilder()
    .ConfigureServices((ctx, s) =>
    {
        // register services for this example
        s.AddScoped<IMyScoped, MyScoped>();
    })
    .Build();

// Initialize bootstrapper
AspNetBootstrapper.Initialize(host); // or HostBridgeWcf.Initialize(host), HB.Initialize(host)

// Diagnostics – prefer ThrowIfCritical() in dev/test
new HostBridgeVerifier()
    .Add(AspNetChecks.VerifyAspNet)   // swap for WcfChecks / WindowsServiceChecks as appropriate
    .ThrowIfCritical();

// Correlation – begin per request/operation/loop iteration
using (Correlation.Begin(HB.Get<ILogger<Program>>()))
{
    // do example work
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

---

## Notes

* **Diagnostics:** always wire a `HostBridgeVerifier` check in this example; fail fast in dev/test, log in prod.
* **Correlation:** controlled by `HostBridge:Correlation:Enabled` in app/web.config. Use `[DisableCorrelation]` on contracts/services to opt-out.
* **Scopes:** validate that scoped services are unique per request/operation, transients are per resolve, and singletons are global.
* **Parallelization:** if this example uses ASP.NET statics (`HttpContext.Current`), test projects must disable xUnit parallelization.

---

## Running

* Build from Visual Studio (Windows required for .NET Framework example apps).
* Or run with:

```bash
dotnet build examples/{ExampleName}/{ExampleName}.csproj
```

* Then launch/debug from IDE.

---

### 🔑 How to use this
- Copy `README.template.md` into the new `examples/{ExampleName}/README.md`.  
- Replace `{ExampleName}` and `{tech stack or host type}`.  
- Swap in the right bootstrapper (`AspNetBootstrapper`, `HostBridgeWcf`, `HB.Initialize`, etc.).  
- Adjust the config snippet (`web.config`, `.svc`, service installer) as needed.  
- Update verifier call to use the correct check (`AspNetChecks`, `WcfChecks`, `WindowsServiceChecks`).  
