using HostBridge.Abstractions;

namespace HostBridge.WindowsService.Tests;

public class HostBridgeServiceBaseTests
{
    private ConfigurableLegacyHost? _host;
    private HostBridgeServiceBase? _svc;
    private Exception? _ex;
    private ThrowingOnStopHost? _throwingOnStop;
    private FaultingStartupHost? _faultingStartup;

    private void GivenServiceWith(ConfigurableLegacyHost host)
    {
        _host = host;
        _svc = new TestService(host) { ServiceName = "HBTestSvc" };
    }

    private void GivenNullHostService()
    {
        _svc = new NullHostService { ServiceName = "HBTestSvc" };
    }

    private void GivenServiceWithThrowingOnStop()
    {
        _throwingOnStop = new ThrowingOnStopHost();
        _svc = new TestService(_throwingOnStop) { ServiceName = "HBTestSvc" };
    }

    private void GivenServiceWithThrowingDispose()
    {
        _svc = new TestService(new ThrowingDisposeHost()) { ServiceName = "HBTestSvc" };
    }

    private void GivenServiceWithFaultingStartup()
    {
        _faultingStartup = new FaultingStartupHost();
        _svc = new TestService(_faultingStartup) { ServiceName = "HBTestSvc" };
    }

    private void GivenServiceWithDefaultHost()
    {
        GivenServiceWith(new ConfigurableLegacyHost());
    }

    private void GivenServiceWithThrowOnStart()
    {
        GivenServiceWith(new ConfigurableLegacyHost { ThrowOnStart = true });
    }

    private void GivenServiceWithStopDelayMs(int ms)
    {
        GivenServiceWith(new ConfigurableLegacyHost { StopDelay = TimeSpan.FromMilliseconds(ms) });
    }

    private void WhenStarting()
    {
        try { _svc!.Start(Array.Empty<string>()); }
        catch (Exception e) { _ex = e; }
    }

    private void WhenStartTwice()
    {
        WhenStarting();
        _ex = null;
        try { _svc!.Start(Array.Empty<string>()); }
        catch (Exception e) { _ex = e; }
    }

    private void WhenStopping()
    {
        try { _svc!.Stop(); }
        catch (Exception e) { _ex = e; }
    }

    private void ThenHostStartCalledOnce()
    {
        _host!.StartCalls.ShouldBe(1);
    }

    private void ThenExitCode1064AndInvalidOpThrown()
    {
        _svc!.ExitCode.Should().Be(1064);
        _ex.Should().BeOfType<InvalidOperationException>();
    }

    private void ThenNoExceptionThrown()
    {
        _ex.Should().BeNull();
    }

    private void ThenHostStopCalledOnceAndDisposed()
    {
        _host!.StopCalls.ShouldBe(1);
        _host!.Disposed.ShouldBeTrue();
    }

    private void ThenHostStopCallsIsZero()
    {
        _host!.StopCalls.ShouldBe(0);
    }

    private void ThenStopObservedCanceledTokenTrue()
    {
        _host!.StopObservedCanceledToken.ShouldBeTrue();
    }

    private void ThenThrowingOnStopNotDisposed()
    {
        _throwingOnStop!.Disposed.Should().BeFalse();
    }

    private void ThenFaultingHostStopCallsZeroAndStartCallsOne()
    {
        _faultingStartup!.StopCalls.Should().Be(0);
        _faultingStartup!.StartCalls.Should().Be(1);
    }

    private sealed class TestService(ILegacyHost host) : HostBridgeServiceBase
    {
        protected override ILegacyHost BuildHost() => host;
    }

    private sealed class NullHostService : HostBridgeServiceBase
    {
        protected override ILegacyHost BuildHost() => null!; // simulate bug
    }

    private sealed class ThrowingOnStopHost : ILegacyHost
    {
        public int StartCalls { get; private set; }
        public int StopCalls { get; private set; }
        public bool Disposed { get; private set; }
        public IServiceProvider ServiceProvider { get; } = new DummySp();

        private sealed class DummySp : IServiceProvider
        {
            public object? GetService(Type serviceType) => null;
        }

        public Task StartAsync(CancellationToken ct = default)
        {
            StartCalls++;
            return Task.CompletedTask;
        }

        public Task StopAsync(CancellationToken ct = default)
        {
            StopCalls++;
            throw new InvalidOperationException("stop boom");
        }

        public void Dispose() { Disposed = true; }
    }

    private sealed class FaultingStartupHost : ILegacyHost
    {
        public int StartCalls { get; private set; }
        public int StopCalls { get; private set; }
        public bool Disposed { get; private set; }
        public IServiceProvider ServiceProvider { get; } = new DummySp();

        private sealed class DummySp : IServiceProvider
        {
            public object? GetService(Type serviceType) => null;
        }

        public Task StartAsync(CancellationToken ct = default)
        {
            StartCalls++;
            // Return a faulted task instead of throwing synchronously
            return Task.FromException(new InvalidOperationException("startup fault"));
        }

        public Task StopAsync(CancellationToken ct = default)
        {
            StopCalls++;
            return Task.CompletedTask;
        }

        public void Dispose() { Disposed = true; }
    }

    private sealed class ThrowingDisposeHost : ILegacyHost
    {
        public IServiceProvider ServiceProvider { get; } = new NoopSp();

        private sealed class NoopSp : IServiceProvider
        {
            public object? GetService(Type serviceType) => null;
        }

        public Task StartAsync(CancellationToken ct = default) => Task.CompletedTask;
        public Task StopAsync(CancellationToken ct = default) => Task.CompletedTask;
        public void Dispose() => throw new InvalidOperationException("dispose boom");
    }

    [Fact]
    public void OnStart_calls_Host_StartAsync()
    {
        this.Given(_ => GivenServiceWithDefaultHost())
            .When(_ => WhenStarting())
            .Then(_ => ThenHostStartCalledOnce())
            .BDDfy();
    }

    [Fact]
    public void OnStart_when_BuildHost_returns_null_sets_exit_code_and_throws()
    {
        this.Given(_ => GivenNullHostService())
            .When(_ => WhenStarting())
            .Then(_ => ThenExitCode1064AndInvalidOpThrown())
            .BDDfy();
    }

    [Fact]
    public void OnStart_when_StartAsync_throws_sets_exit_code_and_throws()
    {
        this.Given(_ => GivenServiceWithThrowOnStart())
            .When(_ => WhenStarting())
            .Then(_ => ThenExitCode1064AndInvalidOpThrown())
            .Then(_ => ThenHostStartCalledOnce())
            .BDDfy();
    }

    [Fact]
    public void Double_Start_is_idempotent_does_not_start_twice()
    {
        this.Given(_ => GivenServiceWithDefaultHost())
            .When(_ => WhenStartTwice())
            .Then(_ => ThenHostStartCalledOnce())
            .BDDfy();
    }

    [Fact]
    public void OnStop_calls_StopAsync_and_Dispose()
    {
        this.Given(_ => GivenServiceWithDefaultHost())
            .When(_ => WhenStarting())
            .When(_ => WhenStopping())
            .Then(_ => ThenHostStopCalledOnceAndDisposed())
            .BDDfy();
    }

    [Fact]
    public void Stop_without_prior_Start_is_noop()
    {
        this.Given(_ => GivenServiceWithDefaultHost())
            .When(_ => WhenStopping())
            .Then(_ => ThenNoExceptionThrown())
            .Then(_ => ThenHostStopCallsIsZero())
            .BDDfy();
    }

    [Fact]
    public void Stop_observes_cancellation_token()
    {
        this.Given(_ => GivenServiceWithStopDelayMs(50))
            .When(_ => WhenStarting())
            .When(_ => WhenStopping())
            .Then(_ => ThenStopObservedCanceledTokenTrue())
            .BDDfy();
    }

    [Fact]
    public void Stop_handles_StopAsync_exceptions_without_throwing()
    {
        this.Given(_ => GivenServiceWithThrowingOnStop())
            .When(_ => WhenStarting())
            .When(_ => WhenStopping())
            .Then(_ => ThenNoExceptionThrown())
            .Then(_ => ThenThrowingOnStopNotDisposed())
            .BDDfy();
    }

    [Fact]
    public void Stop_handles_Dispose_exceptions_without_throwing()
    {
        this.Given(_ => GivenServiceWithThrowingDispose())
            .When(_ => WhenStarting())
            .When(_ => WhenStopping())
            .Then(_ => ThenNoExceptionThrown())
            .BDDfy();
    }

    [Fact]
    public void Startup_fault_is_observed_but_does_not_break_Stop()
    {
        this.Given(_ => GivenServiceWithFaultingStartup())
            .When(_ => WhenStarting())
            .When(_ => WhenStopping())
            .Then(_ => ThenNoExceptionThrown())
            .Then(_ => ThenFaultingHostStopCallsZeroAndStartCallsOne())
            .BDDfy();
    }
        private void WhenShuttingDown()
    {
        try
        {
            var mi = typeof(HostBridgeServiceBase).GetMethod("OnShutdown", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
            mi.ShouldNotBeNull();
            mi!.Invoke(_svc, null);
        }
        catch (Exception e)
        {
            _ex = e;
        }
    }

    [Fact]
    public void OnShutdown_calls_StopAsync_and_Dispose()
    {
        this.Given(_ => GivenServiceWithDefaultHost())
            .When(_ => WhenStarting())
            .When(_ => WhenShuttingDown())
            .Then(_ => ThenHostStopCalledOnceAndDisposed())
            .BDDfy();
    }

    [Fact]
    public void OnShutdown_handles_StopAsync_exceptions_without_throwing()
    {
        this.Given(_ => GivenServiceWithThrowingOnStop())
            .When(_ => WhenStarting())
            .When(_ => WhenShuttingDown())
            .Then(_ => ThenNoExceptionThrown())
            .Then(_ => ThenThrowingOnStopNotDisposed())
            .BDDfy();
    }

    [Fact]
    public void Double_Stop_is_idempotent_does_not_stop_twice()
    {
        this.Given(_ => GivenServiceWithDefaultHost())
            .When(_ => WhenStarting())
            .When(_ => WhenStopping())
            .When(_ => WhenStopping())
            .Then(_ => ThenHostStopCalledOnceAndDisposed())
            .BDDfy();
    }

    [Fact]
    public void ShutdownTimeout_default_is_30_seconds()
    {
        var svc = new TimeoutProbeService(new ConfigurableLegacyHost());
        svc.ServiceName = "HBTestSvc";
        svc.GetShutdownTimeout().ShouldBe(TimeSpan.FromSeconds(30));
    }

    private sealed class TimeoutProbeService(ILegacyHost host) : HostBridgeServiceBase
    {
        protected override ILegacyHost BuildHost() => host;
        public TimeSpan GetShutdownTimeout() => ShutdownTimeout;
    }
}