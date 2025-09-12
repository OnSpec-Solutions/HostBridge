using HostBridge.Abstractions;

namespace HostBridge.Core.Tests;

public class LegacyHostTests
{
    private readonly List<string> _calls = new();
    private ILegacyHost _host = null!;
    private TestLogger<LegacyHost> _logger = null!;

    [Fact]
    public void Given_registered_services_When_host_started_and_stopped_Then_order_is_start_in_registration_order_and_stop_in_reverse()
    {
        this.Given(_ => GivenThreeHostedServices())
            .And(_ => GivenALegacyHost())
            .When(_ => WhenStartingAndStopping())
            .Then(_ => ThenCallsAreInExpectedOrder())
            .BDDfy();
    }

    [Fact]
    public void Given_disposable_provider_When_host_disposed_Then_provider_disposed()
    {
        this.Given(_ => GivenOneDisposableHostedService())
            .And(_ => GivenALegacyHost())
            .When(_ => WhenDisposingHost())
            .Then(_ => ThenProviderDisposedAndHostedDisposed())
            .BDDfy();
    }

    private void GivenThreeHostedServices() => BuildHost(count: 3, disposableHosted: false);
    private void GivenOneDisposableHostedService() => BuildHost(count: 1, disposableHosted: true);

    private void BuildHost(int count, bool disposableHosted)
    {
        _logger = new TestLogger<LegacyHost>();
        var builder = new LegacyHostBuilder()
            .ConfigureServices((_, services) =>
            {
                services.AddSingleton<ILogger<LegacyHost>>(_logger);
                for (int i = 0; i < count; i++)
                {
                    if (disposableHosted)
                    {
                        services.AddSingleton(_calls);
                        services.AddSingleton<IHostedService, DisposableHostedCreated>();
                    }
                    else
                    {
                        services.AddSingleton<IHostedService>(new TrackingHostedService(_calls, $"svc{i}"));
                    }
                }
            });

        _host = builder.Build();
    }

    private void GivenALegacyHost()
    {
        // host already built in BuildHost
    }

    private void WhenStartingAndStopping()
    {
        _host.StartAsync().GetAwaiter().GetResult();
        _host.StopAsync().GetAwaiter().GetResult();
    }

    private void ThenCallsAreInExpectedOrder()
    {
        _calls.Should().ContainInOrder("svc0:start", "svc1:start", "svc2:start", "svc2:stop", "svc1:stop", "svc0:stop");
        _logger.Infos.Should().Contain(s => s.Contains("HostBridge started (3 hosted service(s))."));
        _logger.Infos.Should().Contain(s => s.Contains("HostBridge stopped."));
    }

    private void WhenDisposingHost()
    {
        _host.Dispose();
    }

    private void ThenProviderDisposedAndHostedDisposed()
    {
        // The provider is disposed via host.Dispose(); we verify by ensuring a disposable hosted service was disposed.
        (_calls.Contains("disposed")).Should().BeTrue();
    }

    // Helpers
    private sealed class DisposableHostedCreated : IHostedService, IDisposable
    {
        private readonly List<string> _calls;
        public DisposableHostedCreated(List<string> calls) => _calls = calls;
        public void Dispose() => _calls.Add("disposed");
        public Task StartAsync(CancellationToken ct = default) => Task.CompletedTask;
        public Task StopAsync(CancellationToken ct = default) => Task.CompletedTask;
    }
}