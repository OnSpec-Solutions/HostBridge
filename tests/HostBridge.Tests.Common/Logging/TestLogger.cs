using System;
using System.Collections.Generic;
using Microsoft.Extensions.Logging;

namespace HostBridge.Tests.Common.Logging;

/// <summary>
/// Simple test logger that captures Information-level messages.
/// - By default, messages are stored in the Infos list.
/// - Optionally, an external sink can be provided to append messages into an existing list.
/// </summary>
public sealed class TestLogger<T> : ILogger<T>, ILogger
{
    private readonly List<string>? _externalSink;
    public List<string> Infos { get; } = new();

    public TestLogger()
    {
    }

    public TestLogger(List<string> externalSink)
    {
        _externalSink = externalSink ?? throw new ArgumentNullException(nameof(externalSink));
    }

    IDisposable? ILogger.BeginScope<TState>(TState state) => NullDisposable.Instance;

    public bool IsEnabled(LogLevel logLevel) => true;

    public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception,
        Func<TState, Exception?, string> formatter)
    {
        if (logLevel != LogLevel.Information)
            return;

        var msg = formatter(state, exception);
        if (_externalSink is not null)
        {
            _externalSink.Add(msg);
        }
        else
        {
            Infos.Add(msg);
        }
    }

    private sealed class NullDisposable : IDisposable
    {
        public static readonly NullDisposable Instance = new();
        public void Dispose() { }
    }
}
