namespace HostBridge.Abstractions;

/// <summary>
/// Provides information about the current hosting environment.
/// </summary>
/// <example>
/// var env = new MyEnvImpl("Development");
/// string current = env.EnvironmentName; // "Development"
/// </example>
public interface IHostContext
{
    /// <summary>
    /// Gets the name of the current environment, e.g. "Development", "Staging", or "Production".
    /// </summary>
    string EnvironmentName { get; }
}

/// <summary>
/// Aggregates application configuration and environment context for use during host building.
/// </summary>
/// <param name="configuration">The application configuration built for the host.</param>
/// <param name="environment">The environment context that supplies the environment name.</param>
/// <remarks>Use this type inside configuration and service registration callbacks.</remarks>
/// <example>
/// var ctx = new HostContext(configuration, env);
/// var isProd = ctx.Environment.EnvironmentName == "Production";
/// var conn = ctx.Configuration["connectionStrings:Default"];
/// </example>
public sealed class HostContext(IConfiguration configuration, IHostContext environment)
{
    /// <summary>
    /// Gets the application configuration.
    /// </summary>
    public IConfiguration Configuration => configuration;

    /// <summary>
    /// Gets the environment context.
    /// </summary>
    public IHostContext Environment => environment;
}