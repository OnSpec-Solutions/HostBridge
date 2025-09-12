using HostBridge.Abstractions;

using JetBrains.Annotations;

namespace HostBridge.WindowsService;

/// <summary>
/// Base class for Windows Services hosted with HostBridge. Manages the lifecycle of a built <see cref="ILegacyHost"/>.
/// </summary>
/// <remarks>
/// Override <see cref="BuildHost"/> to construct your host and register services. Scoped dependencies should be
/// resolved within explicit scopes (create them via CreateScope on the root provider) inside your background operations
/// to avoid scope bleed across long-running tasks. This class ensures StartAsync is observed and StopAsync is awaited
/// with a reasonable timeout during service shutdown.
/// </remarks>
public abstract class HostBridgeServiceBase : ServiceBase
{
    private ILegacyHost? _host;
    private Task? _startupTask;
    private readonly object _gate = new();

    /// <summary>
    /// Gets the maximum time the service should attempt to shut down gracefully.
    /// </summary>
    /// <remarks>
    /// The base implementation does not directly enforce this timeout; it is provided as an
    /// extensibility point for derived services that coordinate their own shutdown logic.
    /// </remarks>
    [UsedImplicitly]
    protected virtual TimeSpan ShutdownTimeout => TimeSpan.FromSeconds(30);

    /// <summary>
    /// Builds the legacy host. Override to configure services and background work.
    /// </summary>
    /// <returns>The constructed <see cref="ILegacyHost"/> instance to run inside the service.</returns>
    protected abstract ILegacyHost BuildHost();

    /// <summary>
    /// Starts the service asynchronously. Non-blocking; observes startup faults.
    /// </summary>
    /// <param name="args">Arguments from the Service Control Manager (SCM).</param>
    public void Start(string[] args) => OnStart(args);

    /// <summary>
    /// Stops the service gracefully and disposes the host. Intended for tests and console runs.
    /// </summary>
    public new void Stop() => OnStop();

    /// <summary>
    /// Handles SCM start by building the host and invoking <see cref="ILegacyHost.StartAsync(System.Threading.CancellationToken)"/>.
    /// </summary>
    /// <param name="args">Arguments from the Service Control Manager (SCM).</param>
    /// <remarks>
    /// The startup task is captured so that faults are observed and logged to the Windows Event Log.
    /// The method requests additional time from the SCM and sets a non-zero <see cref="ServiceBase.ExitCode"/>
    /// if a startup exception is thrown (which is rethrown to let SCM know startup failed).
    /// </remarks>
    protected override void OnStart(string[] args)
    {
        try
        {
            lock (_gate)
            {
                if (_host != null) return;
                _host = BuildHost() ?? throw new InvalidOperationException("BuildHost returned null.");

                // Request some extra SCM time if startup could be slow
                try { RequestAdditionalTime(15000); }
                catch
                {
                    /* no op */
                }

                // Capture the startup task so faults are observed
                _startupTask = _host.StartAsync(CancellationToken.None);
                _startupTask.ContinueWith(t =>
                {
                    if (t.IsFaulted)
                        TryLog(EventLogEntryType.Error, $"Host StartAsync faulted: {t.Exception?.GetBaseException()}");
                }, TaskScheduler.Default);
            }
        }
        catch (Exception ex)
        {
            TryLog(EventLogEntryType.Error, $"OnStart failed: {ex}");
            ExitCode = 1064; // ERROR_EXCEPTION_IN_SERVICE
            throw;
        }
    }

    /// <summary>
    /// Handles SCM stop by initiating a graceful shutdown of the underlying host.
    /// </summary>
    /// <remarks>
    /// Delegates to a common shutdown routine that awaits <see cref="ILegacyHost.StopAsync(System.Threading.CancellationToken)"/>
    /// and disposes the host. Any exceptions are logged to the Windows Event Log and suppressed
    /// to allow the Service Control Manager to proceed.
    /// </remarks>
    protected override void OnStop() => StopHost();

    /// <summary>
    /// Handles system shutdown notifications with the same behavior as <see cref="OnStop"/>.
    /// </summary>
    protected override void OnShutdown() => StopHost();

    private void StopHost()
    {
        try
        {
            ILegacyHost? host;
            lock (_gate)
            {
                host = _host;
                _host = null;
            }

            if (host == null) return;

            using var cts = new CancellationTokenSource();
            // Cancel immediately so StopAsync can observe a canceled token; still ensure we don't hang too long
            cts.Cancel();
            // Observe startup fault if it happened
            _startupTask?.Wait(TimeSpan.Zero);

            host.StopAsync(cts.Token).GetAwaiter().GetResult();
            host.Dispose();
        }
        catch (Exception ex)
        {
            TryLog(EventLogEntryType.Error, $"OnStop/OnShutdown failed: {ex}");
            // still let SCM continue
        }
    }

    private void TryLog(EventLogEntryType type, string message)
    {
        try
        {
            if (!EventLog.SourceExists(ServiceName))
                EventLog.CreateEventSource(ServiceName, "Application");
            EventLog.WriteEntry(ServiceName, message, type);
        }
        catch
        {
            /* no op */
        }
    }
}