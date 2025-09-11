using System;
using System.Threading;
using System.Threading.Tasks;
using HostBridge.Abstractions;

namespace HostBridge.Tests.Common.Fakes;

// Legacy host test double with configurable behaviors and call counters.
public sealed class ConfigurableLegacyHost(CancellationToken? stopToken = null) : ILegacyHost
{
    private CancellationToken? _stopToken = stopToken;
    public int StartCalls { get; private set; }
    public int StopCalls { get; private set; }
    public bool Disposed { get; private set; }
    public bool ThrowOnStart { get; set; }
    public TimeSpan StopDelay { get; set; } = TimeSpan.Zero;
    public bool StopObservedCanceledToken => _stopToken is { IsCancellationRequested: true };

    private sealed class DummySp : IServiceProvider { public object? GetService(Type serviceType) => null; }
    public IServiceProvider ServiceProvider { get; } = new DummySp(); 
    public Task StartAsync(CancellationToken ct = default)
    {
        StartCalls++;
        return ThrowOnStart ? throw new InvalidOperationException("boom") : Task.CompletedTask;
    }

    public async Task StopAsync(CancellationToken ct = default)
    {
        _stopToken = ct;
        StopCalls++;
        if (StopDelay > TimeSpan.Zero)
        {
            try { await Task.Delay(StopDelay, ct); } catch { /* ignore */ }
        }
    }

    public void Dispose() => Disposed = true;
}
