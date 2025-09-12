using HostBridge.Abstractions;

namespace HostBridge.Core;

/// <summary>
/// A trivial hosted service that writes a periodic heartbeat log entry.
/// </summary>
/// <remarks>Intended as a template or a keep-alive for legacy hosts.</remarks>
/// <example>
/// <code>services.AddHostedService&lt;HeartbeatService&gt;();</code>
/// </example>
public sealed class HeartbeatService(ILogger<HeartbeatService> log) : IHostedService, IDisposable
{
    private Timer? _timer;

    /// <inheritdoc />
    public Task StartAsync(CancellationToken ct = default)
    {
        _timer = new Timer(_ => log.LogInformation("hb"), null, 0, 15000);
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task StopAsync(CancellationToken ct = default)
    {
        _timer?.Dispose();
        _timer = null;
        return Task.CompletedTask;
    }
        
    /// <summary>
    /// Disposes timer resources.
    /// </summary>
    public void Dispose()
    {
        _timer?.Dispose();
    }
}