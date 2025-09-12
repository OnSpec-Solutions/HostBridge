using HostBridge.Abstractions;

namespace HostBridge.Core.Tests;

public class LegacyHostRunExtensionsTests
{
    [Fact]
    public async Task RunAsync_null_host_throws()
    {
        ILegacyHost? host = null;
        Func<Task> act = () => LegacyHostRunExtensions.RunAsync(host!);
        await act.Should().ThrowAsync<ArgumentNullException>();
    }

    [Fact]
    public async Task RunAsync_starts_waits_for_cancellation_then_stops_and_disposes()
    {
        var fake = new ConfigurableLegacyHost();
        using var cts = new CancellationTokenSource();

        var task = fake.RunAsync(cts.Token);
        await Task.Delay(20); // allow StartAsync to run
        fake.StartCalls.Should().Be(1);
        fake.StopCalls.Should().Be(0);
        fake.Disposed.Should().BeFalse();

        cts.Cancel();
        await task; // should complete after cancellation and stop

        fake.StopCalls.Should().Be(1);
        fake.Disposed.Should().BeTrue();
    }

    [Fact]
    public async Task RunAsync_with_timeout_uses_cancellation_on_stop()
    {
        var fake = new ConfigurableLegacyHost { StopDelay = TimeSpan.FromMilliseconds(100) };
        using var cts = new CancellationTokenSource();

        var run = fake.RunAsync(cts.Token, shutdownTimeout: TimeSpan.FromMilliseconds(10));
        await Task.Delay(20);
        cts.Cancel();
        await run;

        fake.StopCalls.Should().Be(1);
        fake.StopObservedCanceledToken.Should().BeTrue();
    }

    [Fact]
    public async Task RunConsoleAsync_null_host_throws()
    {
        ILegacyHost? host = null;
        Func<Task> act = () => LegacyHostRunExtensions.RunConsoleAsync(host!);
        await act.Should().ThrowAsync<ArgumentNullException>();
    }

    [Fact]
    public async Task RunConsoleAsync_when_StartAsync_throws_still_calls_Stop_and_Dispose()
    {
        var fake = new ConfigurableLegacyHost { ThrowOnStart = true };
        Func<Task> act = () => fake.RunConsoleAsync();
        await act.Should().ThrowAsync<InvalidOperationException>();
        fake.StopCalls.Should().Be(1);
        fake.Disposed.Should().BeTrue();
    }
}