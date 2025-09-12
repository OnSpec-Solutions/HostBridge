using HostBridge.Abstractions;

namespace HostBridge.Core.Tests;

public class LegacyHostBuilderTests
{
    private readonly List<string> _logs = new();
    private readonly List<string> _calls = new();
    private string _env = string.Empty;
    private string? _cfgValue;

    [Fact]
    public void Given_builder_When_configured_and_built_Then_host_runs_and_services_receive_calls()
    {
        this.Given(_ => GivenConfiguredBuilder())
            .When(_ => WhenBuildingAndRunningHost())
            .Then(_ => ThenHostedServiceWasCalledAndEnvAndConfigPropagated())
            .BDDfy();
    }

    private void GivenConfiguredBuilder()
    {
        var builder = new LegacyHostBuilder()
            .UseEnvironment("Dev")
            .ConfigureAppConfiguration(cfg =>
                cfg.AddInMemoryCollection(new Dictionary<string, string?> { ["k1"] = "v1" }))
            .ConfigureLogging(lb => lb.AddProvider(new ListLoggerProvider(_logs)))
            .ConfigureServices((ctx, services) =>
            {
                _env = ctx.Environment.EnvironmentName;
                _cfgValue = ctx.Configuration["k1"];
                services.AddSingleton(_calls);
                services.AddHostedService<TestHosted>();
            });

        _host = builder.Build();
    }

    private ILegacyHost _host = null!;

    private void WhenBuildingAndRunningHost()
    {
        _host.StartAsync().GetAwaiter().GetResult();
        _host.StopAsync().GetAwaiter().GetResult();
        _host.Dispose();
    }

    private void ThenHostedServiceWasCalledAndEnvAndConfigPropagated()
    {
        _calls.Should().ContainInOrder("start", "stop");
        _env.Should().Be("Dev");
        _cfgValue.Should().Be("v1");
        _logs.Should().NotBeEmpty(); // logger plugged in
    }

    [UsedImplicitly]
    private sealed class TestHosted(List<string> calls) : IHostedService
    {
        public Task StartAsync(CancellationToken ct = default)
        {
            calls.Add("start");
            return Task.CompletedTask;
        }

        public Task StopAsync(CancellationToken ct = default)
        {
            calls.Add("stop");
            return Task.CompletedTask;
        }
    }

    private sealed class ListLoggerProvider(List<string> sink) : ILoggerProvider
    {
        public ILogger CreateLogger(string categoryName) => new ListLogger(sink);
        public void Dispose() { }

        private sealed class ListLogger(List<string> sink) : ILogger
        {
            public IDisposable BeginScope<TState>(TState state) => NullDisposable.Instance;
            public bool IsEnabled(LogLevel logLevel) => true;

            public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception,
                Func<TState, Exception?, string> formatter)
            {
                sink.Add(formatter(state, exception));
            }

            private sealed class NullDisposable : IDisposable
            {
                public static readonly NullDisposable Instance = new();
                public void Dispose() { }
            }
        }
    }
}