using HostBridge.Abstractions;
using HostBridge.Core;

namespace HostBridge.Core.Tests;

public class LegacyHostRunExtensionsTests
{
    private sealed class FakeHost : ILegacyHost
    {
        public int StartCalls { get; private set; }
        public int StopCalls { get; private set; }
        public int DisposeCount { get; private set; }
        public int StopWithTimeoutCalls { get; private set; }
        public int StopWithoutTimeoutCalls { get; private set; }

        public IServiceProvider ServiceProvider { get; } = new ServiceCollection().BuildServiceProvider();

        public Task StartAsync(CancellationToken ct = default)
        {
            StartCalls++;
            return Task.CompletedTask;
        }

        public async Task StopAsync(CancellationToken ct = default)
        {
            StopCalls++;
            if (ct.CanBeCanceled)
            {
                StopWithTimeoutCalls++;
                try
                {
                    // Wait until canceled to simulate honoring timeout token
                    await Task.Delay(Timeout.Infinite, ct);
                }
                catch (OperationCanceledException)
                {
                    // expected
                }
            }
            else
            {
                StopWithoutTimeoutCalls++;
            }
        }

        public void Dispose() => DisposeCount++;
    }

    [Fact]
    public async Task RunAsync_starts_waits_for_cancellation_then_stops_and_disposes_without_timeout()
    {
        var host = new FakeHost();
        using var cts = new CancellationTokenSource();

        var runTask = host.RunAsync(cts.Token, shutdownTimeout: null);
        // allow StartAsync to run
        await Task.Delay(10);
        host.StartCalls.Should().Be(1);

        // Cancel and wait for shutdown
        cts.Cancel();
        await runTask;

        host.StopCalls.Should().Be(1);
        host.StopWithoutTimeoutCalls.Should().Be(1);
        host.StopWithTimeoutCalls.Should().Be(0);
        host.DisposeCount.Should().Be(1);
    }

    [Fact]
    public async Task RunAsync_uses_timeout_token_when_timeout_specified()
    {
        var host = new FakeHost();
        using var cts = new CancellationTokenSource();

        var runTask = host.RunAsync(cts.Token, shutdownTimeout: TimeSpan.FromMilliseconds(25));
        await Task.Delay(10);
        host.StartCalls.Should().Be(1);

        // Trigger cancellation of the run loop
        cts.Cancel();
        await runTask;

        host.StopCalls.Should().Be(1);
        host.StopWithTimeoutCalls.Should().Be(1);
        host.DisposeCount.Should().Be(1);
    }
}