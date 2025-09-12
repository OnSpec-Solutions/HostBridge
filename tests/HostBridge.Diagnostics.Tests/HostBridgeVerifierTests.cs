using System;
using System.Collections.Generic;
using System.Linq;
using FluentAssertions;
using HostBridge.Diagnostics;
using Microsoft.Extensions.Logging;
using Xunit;

namespace HostBridge.Diagnostics.Tests;

public class HostBridgeVerifierTests
{
    [Fact]
    public void ThrowIfCritical_throws_when_critical_or_error_present()
    {
        var v = new HostBridgeVerifier()
            .Add(() => new[]
            {
                new DiagnosticResult("HB-OK", Severity.Info, "info"),
                new DiagnosticResult("HB-WARN", Severity.Warning, "warn"),
                new DiagnosticResult("HB-ERR", Severity.Error, "err"),
            });

        var act = () => v.ThrowIfCritical();
        act.Should().Throw<InvalidOperationException>()
           .WithMessage("*HB-ERR*");
    }

    [Fact]
    public void Log_writes_each_severity_to_expected_level_and_does_not_throw()
    {
        var sink = new List<(LogLevel level, string message)>();
        var logger = new ListLogger(sink);
        var v = new HostBridgeVerifier()
            .Add(() => new[]
            {
                new DiagnosticResult("HB-I", Severity.Info, "info"),
                new DiagnosticResult("HB-W", Severity.Warning, "warn"),
                new DiagnosticResult("HB-E", Severity.Error, "err"),
                new DiagnosticResult("HB-C", Severity.Critical, "crit"),
            });

        v.Log(logger);

        sink.Select(s => s.level).Should().BeEquivalentTo(new[]
        {
            LogLevel.Information, LogLevel.Warning, LogLevel.Error, LogLevel.Critical
        });
        sink.All(s => s.message.Contains("HB-")).Should().BeTrue();
    }

    [Fact]
    public void Safe_wrapper_emits_error_when_check_throws_and_ignores_null_enumerable()
    {
        var v = new HostBridgeVerifier()
            .Add(() => throw new InvalidOperationException("boom"))
            .Add(() => Enumerable.Empty<DiagnosticResult>())
            .Add(() => null!);

        var results = v.Run();

        results.Should().Contain(r => r.ToString().Contains("Verifier crashed") && r.Severity == Severity.Error);
        // Nothing else added by the other checks
        results.Count.Should().Be(1);
    }

    private sealed class ListLogger(List<(LogLevel level, string message)> sink) : ILogger
    {
        public IDisposable BeginScope<TState>(TState state) => NullScope.Instance;
        public bool IsEnabled(LogLevel logLevel) => true;
        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
        {
            sink.Add((logLevel, formatter(state, exception)));
        }

        private sealed class NullScope : IDisposable
        {
            public static readonly NullScope Instance = new();
            public void Dispose() { }
        }
    }
}
