[//]: # (./src/HostBridge.Options.Config/README.md)

# HostBridge.Options.Config

💡 *“Because `app.config` and `web.config` aren’t going away tomorrow.”*

This package makes your dusty XML configs (`app.config` / `web.config`) play nice with
`Microsoft.Extensions.Configuration` and `IOptions<T>` — without a rewrite.

---

## API Surface

```csharp
IConfigurationBuilder AddHostBridgeAppConfig(this IConfigurationBuilder builder)
````

* Reads `<appSettings>` and `<connectionStrings>` from classic config.
* Keys appear in the modern configuration system as:

    * `appSettings:MyKey` → value
    * `connectionStrings:Default` → connection string

---

## Wiring

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

**Global.asax.cs**

```csharp
protected void Application_Start()
{
    var host = new LegacyHostBuilder()
        .ConfigureAppConfiguration(cfg =>
        {
            // pull in appSettings/connectionStrings
            cfg.AddHostBridgeAppConfig();

            // add JSON or other providers as overrides if desired
            cfg.AddJsonFile("appsettings.json", optional: true);
        })
        .ConfigureServices((ctx, services) =>
        {
            // bind options from classic config
            services.Configure<MyOptions>(ctx.Configuration.GetSection("My"));
        })
        .Build();

    AspNetBootstrapper.Initialize(host);
}
```

---

## How it works

* Values from `<appSettings>` and `<connectionStrings>` are injected into the standard
  `IConfiguration` pipeline.
* Later providers (e.g. JSON, env vars) can override them — standard `IConfiguration`
  precedence applies.
* Options binding works the same way as in .NET Core:

  ```csharp
  public sealed class MyOptions
  {
      public FeatureOptions Feature { get; set; } = new();
  }
  ```

---

## Diagnostics Tip

To verify critical keys are present, add a custom check:

```csharp
verifier.Add(() =>
{
    var cfg = host.Services.GetRequiredService<IConfiguration>();
    if (string.IsNullOrEmpty(cfg["connectionStrings:Default"]))
        yield return new DiagnosticResult(
            "HB-CONFIG-001",
            Severity.Error,
            "Missing Default connection string in app/web.config.");
});
```

---

## Notes

* Supported TFMs: `netstandard2.0`, `net472`, `net48`.
* Plays nicely with JSON, environment variables, and other providers.
* Useful for bridging **modern DI + Options** into legacy app domains.
* ⚡ See also:
    * [HostBridge.Diagnostics](../HostBridge.Diagnostics/README.md) – fail fast on wiring mistakes
    * [HostBridge.Core](../HostBridge.Core/README.md) – `HB` accessor and ambient scopes
