using HostBridge.Abstractions;

namespace HostBridge.Wcf;

/*
// In your Global.asax.cs
protected void Application_Start()
{
    var host = new LegacyHostBuilder()
        .ConfigureServices((ctx, s) =>
        {
            s.AddOptions();
            // register your WCF service types with the desired lifetimes
            s.AddTransient<MyService>();              // service class itself should be transient
            s.AddScoped<IMyScopedDep, MyScopedDep>(); // per-operation
            s.AddSingleton<ISingletonDep, SingletonDep>();
        })
        .Build();

    HostBridge.Wcf.HostBridgeWcf.Initialize(host); // exposes RootServices to the provider
    // Optionally start hosted services here if you have any
}

// In your service implementation
[ServiceBehavior(InstanceContextMode = InstanceContextMode.PerCall, ConcurrencyMode = ConcurrencyMode.Multiple)]
public class MyService : IMyService
{
    private readonly IMyScopedDep _dep;
    public MyService(IMyScopedDep dep) => _dep = dep;
    // ...
}

<!-- In your web.config -->
<service name="MyNs.MyService" behaviorConfiguration="..."
         factory="HostBridge.Wcf.DiServiceHostFactory">
  <endpoint address="" binding="basicHttpBinding" contract="MyNs.IMyService" />
</service>
 */

/// <summary>
/// Bootstraps HostBridge’s WCF integration by exposing the application’s root <see cref="IServiceProvider"/>.
/// </summary>
/// <remarks>
/// Usage:
/// 1) Build your <see cref="ILegacyHost"/> during application startup (e.g., Global.asax Application_Start).
/// 2) Call <see cref="Initialize"/> with the built host to capture the root provider.
/// 3) Configure your WCF service to use <see cref="DiServiceHostFactory"/> so that a new
///    <see cref="Microsoft.Extensions.DependencyInjection.IServiceScope"/> is created per operation, ensuring scoped
///    dependencies are isolated to a single request.
/// </remarks>
public static class HostBridgeWcf
{
    /// <summary>
    /// Gets the root service provider for the application.
    /// </summary>
    public static IServiceProvider? RootServices { get; private set; }

    /// <summary>
    /// Initializes HostBridge for WCF by capturing the root provider from the built host.
    /// </summary>
    /// <param name="host">The built legacy host.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="host"/> is null.</exception>
    public static void Initialize(ILegacyHost? host) =>
        RootServices = host?.ServiceProvider ?? throw new ArgumentNullException(nameof(host));
}