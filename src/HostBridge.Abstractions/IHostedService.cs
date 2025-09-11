using System.Threading;
using System.Threading.Tasks;

using JetBrains.Annotations;

namespace HostBridge.Abstractions;

/// <summary>
/// Defines a long-running background service with explicit start and stop semantics.
/// </summary>
/// <example>
/// public sealed class Worker : IHostedService {
///     public Task StartAsync(CancellationToken ct = default) { /* start timers, etc. */ return Task.CompletedTask; }
///     public Task StopAsync(CancellationToken ct = default) { /* cleanup */ return Task.CompletedTask; }
/// }
/// </example>
public interface IHostedService
{
    /// <summary>
    /// Starts the service.
    /// </summary>
    /// <param name="ct">Propagation token to observe while awaiting the task.</param>
    Task StartAsync([UsedImplicitly] CancellationToken ct = default);

    /// <summary>
    /// Attempts to gracefully stop the service.
    /// </summary>
    /// <param name="ct">Propagation token to observe while awaiting the task.</param>
    Task StopAsync([UsedImplicitly] CancellationToken ct = default);
}