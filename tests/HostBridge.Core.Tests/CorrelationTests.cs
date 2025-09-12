using System;
using System.Collections.Generic;
using FluentAssertions;
using HostBridge.Core;
using Microsoft.Extensions.Logging;
using Xunit;

namespace HostBridge.Core.Tests;

public class CorrelationTests
{
    [Fact]
    public void Begin_without_logger_sets_and_clears_accessed_id_and_is_idempotent_on_dispose()
    {
        var accessor = new CorrelationAccessor();
        accessor.CorrelationId.Should().BeNull();

        var d = Correlation.Begin(null, "abc123", headerName: "X-Test");
        accessor.CorrelationId.Should().Be("abc123");

        d.Dispose();
        accessor.CorrelationId.Should().BeNull();

        // Second dispose is a no-op
        d.Dispose();
        accessor.CorrelationId.Should().BeNull();
    }

    [Fact]
    public void Begin_with_logger_creates_logger_scope_and_disposes_in_reverse_order()
    {
        var accessor = new CorrelationAccessor();
        var logger = new TestLogger();

        using (var d = Correlation.Begin(logger, null, headerName: "X-Correlation-Id"))
        {
            accessor.CorrelationId.Should().NotBeNull();
            logger.LastScopeState.Should().NotBeNull();
            logger.LastScopeDisposed.Should().BeFalse();
        }

        // After using: accessor cleared and logger scope disposed exactly once
        accessor.CorrelationId.Should().BeNull();
        logger.LastScopeDisposed.Should().BeTrue();
        logger.DisposeCount.Should().Be(1);

        // Ensure idempotent dispose
        logger.Reset();
        var disp = Correlation.Begin(logger, "id2");
        disp.Dispose();
        disp.Dispose();
        logger.DisposeCount.Should().Be(1);
    }

    private sealed class TestLogger : ILogger
    {
        public bool LastScopeDisposed { get; private set; }
        public int DisposeCount { get; private set; }
        public object? LastScopeState { get; private set; }

        public void Reset()
        {
            LastScopeDisposed = false;
            DisposeCount = 0;
            LastScopeState = null;
        }

        private sealed class Scope(TestLogger owner) : IDisposable
        {
            private int _disposed;
            public void Dispose()
            {
                if (Interlocked.Exchange(ref _disposed, 1) == 1) return;
                owner.LastScopeDisposed = true;
                owner.DisposeCount++;
            }
        }

        public IDisposable BeginScope<TState>(TState state)
        {
            LastScopeState = state;
            return new Scope(this);
        }

        public bool IsEnabled(LogLevel logLevel) => true;
        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception,
            Func<TState, Exception?, string> formatter)
        {
        }
    }
}
