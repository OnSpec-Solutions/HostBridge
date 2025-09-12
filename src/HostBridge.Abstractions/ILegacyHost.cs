namespace HostBridge.Abstractions;

/// <summary>
/// Represents a simple host that can start and stop background services and exposes a service provider.
/// </summary>
/// <remarks>
/// Dispose the host to release its underlying service provider.
/// </remarks>
/// <example>
/// using var host = builder.Build();
/// await host.StartAsync();
/// // resolve services
/// await host.StopAsync();
/// </example>
public interface ILegacyHost : IDisposable
{
    /// <summary>
    /// Gets the root <see cref="IServiceProvider"/> for resolving services.
    /// </summary>
    IServiceProvider ServiceProvider { get; }

    /// <summary>
    /// Starts the host and any registered <see cref="IHostedService"/> instances.
    /// </summary>
    /// <param name="ct">Propagation token to observe while awaiting the task.</param>
    Task StartAsync(CancellationToken ct = default);

    /// <summary>
    /// Attempts to gracefully stop the host and its hosted services.
    /// </summary>
    /// <param name="ct">Propagation token to observe while awaiting the task.</param>
    Task StopAsync(CancellationToken ct = default);
}