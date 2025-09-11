using System;
using System.Threading;
using System.Threading.Tasks;

using HostBridge.Abstractions;

using JetBrains.Annotations;

namespace HostBridge.Core;

/// <summary>
/// Convenience run/stop helpers for <see cref="ILegacyHost"/>.
/// </summary>
/// <example>
/// using var host = builder.Build();
/// using var cts = new CancellationTokenSource();
/// var run = host.RunAsync(cts.Token, TimeSpan.FromSeconds(10));
/// cts.Cancel();
/// await run;
/// </example>
[UsedImplicitly]
public static class LegacyHostRunExtensions
{
    /// <summary>
    /// Starts the host, waits until the token is canceled, then stops the host.
    /// Safe for services and any non-console environment.
    /// </summary>
    /// <param name="host">The host to run.</param>
    /// <param name="token">A cancellation token that signals when to stop.</param>
    /// <param name="shutdownTimeout">Optional timeout to apply to StopAsync.</param>
    /// <returns>A task that completes when the host has stopped.</returns>
    [UsedImplicitly]
    public static async Task RunAsync(this ILegacyHost host, CancellationToken token = default, TimeSpan? shutdownTimeout = null)
    {
        if (host is null)
        {
            throw new ArgumentNullException(nameof(host));
        }

        await host.StartAsync(token).ConfigureAwait(false);

        var tcs = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        using var tokenRegistration = token.Register(() => tcs.TrySetResult(true));
        await tcs.Task.ConfigureAwait(false);

        await StopWithTimeout(host, shutdownTimeout).ConfigureAwait(false);
    }

    /// <summary>
    /// Runs the host in a console-friendly manner: hooks Ctrl+C and ProcessExit,
    /// cancels an internal token, and performs a single, graceful shutdown.
    /// </summary>
    /// <param name="host">The host instance to run.</param>
    /// <param name="shutdownTimeout">Optional timeout applied to StopAsync during shutdown.</param>
    /// <returns>A task that completes when the host has stopped.</returns>
    /// <example>
    /// using var host = new LegacyHostBuilder().Build();
    /// await host.RunConsoleAsync(TimeSpan.FromSeconds(10));
    /// // Press Ctrl+C to trigger shutdown
    /// </example>
    [UsedImplicitly]
    public static async Task RunConsoleAsync(this ILegacyHost host, TimeSpan? shutdownTimeout = null)
    {
        if (host is null) throw new ArgumentNullException(nameof(host));

        using (var cts = new CancellationTokenSource())
        {
            ConsoleCancelEventHandler? onCancelKeyPress = null;
            EventHandler? onProcessExit = null;

            try
            {
                onCancelKeyPress = (_, e) =>
                {
                    e.Cancel = true; // keep process alive until we stop gracefully
                    cts.Cancel();
                };
                Console.CancelKeyPress += onCancelKeyPress;

                onProcessExit = (_, _) => cts.Cancel();
                AppDomain.CurrentDomain.ProcessExit += onProcessExit;

                await host.StartAsync(cts.Token).ConfigureAwait(false);

                var tcs = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
                using var cancellationRegistration = cts.Token.Register(() => tcs.TrySetResult(true));
                await tcs.Task.ConfigureAwait(false);
            }
            finally
            {
                if (onCancelKeyPress is not null)
                {
                    Console.CancelKeyPress -= onCancelKeyPress;
                }

                if (onProcessExit is not null)
                {
                    AppDomain.CurrentDomain.ProcessExit -= onProcessExit;
                }

                await StopWithTimeout(host, shutdownTimeout).ConfigureAwait(false);
            }
        }
    }

    /// <summary>
    /// Attempts to stop the host, applying a cancellation timeout if provided, then disposes it.
    /// </summary>
    /// <param name="host">The host to stop.</param>
    /// <param name="timeout">Optional timeout for StopAsync; if null or non-positive, no timeout is applied.</param>
    private static async Task StopWithTimeout(ILegacyHost host, TimeSpan? timeout)
    {
        try
        {
            switch (timeout)
            {
                case { } t when t > TimeSpan.Zero:
                    {
                        using var cts = new CancellationTokenSource(t);
                        await host.StopAsync(cts.Token).ConfigureAwait(false);
                        break;
                    }
                default:
                    await host.StopAsync().ConfigureAwait(false);
                    break;
            }
        }
        finally
        {
            host.Dispose();
        }
    }
}