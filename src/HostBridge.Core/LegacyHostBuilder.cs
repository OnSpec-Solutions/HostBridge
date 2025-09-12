using HostBridge.Abstractions;

namespace HostBridge.Core;

/// <summary>
/// A minimal host builder for legacy-style applications that need configuration, DI, logging and hosted services.
/// </summary>
/// <remarks>
/// This builder mirrors a small subset of the generic host to keep legacy apps lightweight.
/// </remarks>
/// <example>
/// <code>
/// var host = new LegacyHostBuilder()
///     .UseEnvironment("Development")
///     .ConfigureAppConfiguration(cfg => cfg.AddJsonFile("appsettings.json", optional: true))
///     .ConfigureLogging(lb => lb.AddConsole())
///     .ConfigureServices((ctx, services) =>
///     {
///         services.AddHostedService&lt;HeartbeatService&gt;();
///     })
///     .Build();
/// await host.StartAsync();
/// await host.StopAsync();
/// </code>
/// </example>
public sealed class LegacyHostBuilder
{
    private readonly IServiceCollection _services = new ServiceCollection();
    private readonly ConfigurationBuilder _cfg = new();
    private Action<HostContext, IServiceCollection>? _configureServices;
    private string _envName = "Production";

    /// <summary>
    /// Sets the environment name for the host (e.g. Development, Staging, Production).
    /// </summary>
    /// <param name="name">Environment name.</param>
    public LegacyHostBuilder UseEnvironment(string name)
    {
        _envName = name;
        return this;
    }

    /// <summary>
    /// Configures the application configuration pipeline.
    /// </summary>
    /// <param name="configure">A callback to populate the <see cref="IConfigurationBuilder"/>.</param>
    public LegacyHostBuilder ConfigureAppConfiguration(Action<IConfigurationBuilder> configure)
    {
        configure(_cfg);
        return this;
    }

    /// <summary>
    /// Configures logging services.
    /// </summary>
    /// <param name="configure">A callback to register providers using <see cref="ILoggingBuilder"/>.</param>
    public LegacyHostBuilder ConfigureLogging(Action<ILoggingBuilder> configure)
    {
        _services.AddLogging(configure);
        return this;
    }

    /// <summary>
    /// Registers application services.
    /// </summary>
    /// <param name="configure">Callback that receives the <see cref="HostContext"/> and service collection.</param>
    public LegacyHostBuilder ConfigureServices(Action<HostContext, IServiceCollection> configure)
    {
        _configureServices = configure;
        return this;
    }

    /// <summary>
    /// Builds the host instance.
    /// </summary>
    /// <returns>An <see cref="ILegacyHost"/> ready to run.</returns>
    public ILegacyHost Build()
    {
        var configuration = _cfg.Build();
        var context = new HostContext
        (
            configuration,
            new SimpleEnv(_envName)
        );

        _services.AddOptions();

        _configureServices?.Invoke(context, _services);

        var sp = _services.BuildServiceProvider();
        return new LegacyHost(sp, sp.GetRequiredService<ILogger<LegacyHost>>());
    }

    /// <summary>
    /// Lightweight environment implementation used by the builder.
    /// </summary>
    private sealed class SimpleEnv(string env) : IHostContext
    {
        /// <summary>
        /// Gets the environment name.
        /// </summary>
        public string EnvironmentName => env;
    }
}