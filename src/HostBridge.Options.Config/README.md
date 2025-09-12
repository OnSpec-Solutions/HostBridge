# HostBridge.Options.Config

💡 *“Because `app.config` and `web.config` aren’t going away tomorrow.”*

This package makes your dusty XML configs play nice with `Microsoft.Extensions.Configuration` and `IOptions<T>`.

### Why you want it

You’ve got `appSettings` and `connectionStrings` in `web.config`. You want to bind them to POCO options like you do in .NET Core. Without rewriting all the configs. Done.

### Wire-up

**web.config**

```xml
<configuration>
  <appSettings>
    <add key="My:Feature:Enabled" value="true" />
  </appSettings>
  <connectionStrings>
    <add name="Default" connectionString="Server=.;Database=MyDb;Trusted_Connection=True;" />
  </connectionStrings>
</configuration>
```

**C#**

```csharp
var cfg = new ConfigurationBuilder()
    .AddHostBridgeAppConfig() // from HostBridge.Options.Config
    .Build();

services.Configure<MyOptions>(cfg);

var enabled = cfg["My:Feature:Enabled"];         // "true"
var cs = cfg["connectionStrings:Default"];      // connection string
```

### Keys exposed

* `appSettings:KeyName` → value
* `connectionStrings:Default` → connection string

### Notes

* Works in `net472`, `net48`, and `netstandard2.0`.
* Plays nicely with JSON overrides if you add `.AddJsonFile("appsettings.json")`.

---

# HostBridge.Core

💡 *“A static accessor that doesn’t make you hate yourself.”*

Sometimes you need a static handle into DI (console apps, Windows Services). `HB` gives you:

* `HB.Initialize(host)` — set it once
* `HB.Get<T>()` / `HB.TryGet<T>()` — resolve without ceremony
* `HB.CreateScope()` / `HB.BeginScope()` — spin up scoped lifetimes
* `HB.Current` — ambient provider, flows across `async/await`

### Quick start

```csharp
var services = new ServiceCollection();
services.AddScoped<IMyScoped, MyScoped>();
var sp = services.BuildServiceProvider();

var host = new LegacyHost(sp);
HB.Initialize(host);

using (HB.BeginScope())
{
    var svc = HB.Get<IMyScoped>(); // resolved from scope
}
```

### Notes

* Call `Initialize` once per process.
* Accessing `HB.Root` before init throws (better than nulls).
* Scoped services resolved inside `BeginScope()` are disposed when the scope ends.
* Use at app edges — still prefer constructor injection in real services.
