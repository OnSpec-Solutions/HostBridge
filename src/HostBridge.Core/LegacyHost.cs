using HostBridge.Abstractions;

namespace HostBridge.Core;

/// <summary>
/// Default <see cref="ILegacyHost"/> implementation that starts/stops registered
/// <see cref="IHostedService"/> instances and exposes the application <see cref="IServiceProvider"/>.
/// </summary>
internal sealed class LegacyHost(IServiceProvider sp, ILogger<LegacyHost> log) : ILegacyHost
{
    private readonly ILogger _log = log;
    private readonly List<IHostedService> _hostedServices = [..sp.GetServices<IHostedService>()];

    /// <summary>
    /// Gets the root service provider for the application.
    /// </summary>
    public IServiceProvider ServiceProvider => sp;

    /// <summary>
    /// Starts all registered <see cref="IHostedService"/> instances.
    /// </summary>
    /// <param name="ct">A cancellation token to observe.</param>
    public async Task StartAsync(CancellationToken ct = default)
    {
        foreach (var svc in _hostedServices)
        {
            await svc.StartAsync(ct).ConfigureAwait(false);
        }

        _log.LogInformation("HostBridge started ({Count} hosted service(s)).", _hostedServices.Count);
    }

    /// <summary>
    /// Stops hosted services in reverse registration order.
    /// </summary>
    /// <param name="ct">A cancellation token to observe.</param>
    public async Task StopAsync(CancellationToken ct = default)
    {
        for (var i = _hostedServices.Count - 1; i >= 0; i--)
        {
            await _hostedServices[i].StopAsync(ct).ConfigureAwait(false);
        }

        _log.LogInformation("HostBridge stopped.");
    }

    /// <summary>
    /// Disposes the underlying service provider if it implements <see cref="IDisposable"/>.
    /// </summary>
    public void Dispose()
    {
        if (ServiceProvider is IDisposable d)
        {
            d.Dispose();
        }
    }
}