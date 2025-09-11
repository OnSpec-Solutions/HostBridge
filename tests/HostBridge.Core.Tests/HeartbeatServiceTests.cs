using System.Collections.Generic;
using System.Threading.Tasks;
using FluentAssertions;

using HostBridge.Tests.Common.Logging;
using TestStack.BDDfy;
using Xunit;

namespace HostBridge.Core.Tests;

public class HeartbeatServiceTests
{
    private readonly List<string> _messages = new();
    private TestLogger<HeartbeatService> _logger = null!;
    private HeartbeatService _sut = null!;

    [Fact]
    public async Task Given_HeartbeatService_When_started_then_it_logs_and_can_be_stopped_and_disposed()
    {
        this.Given(_ => GivenALogger())
            .And(_ => GivenASut())
            .When(_ => WhenStarting())
            .Then(_ => ThenItLogsAtLeastOnce())
            .And(_ => ThenCanBeStoppedAndDisposedTwiceWithoutError())
            .BDDfy();
    }

    private void GivenALogger()
    {
        _logger = new TestLogger<HeartbeatService>(_messages);
    }

    private void GivenASut()
    {
        _sut = new HeartbeatService(_logger);
    }

    private void WhenStarting()
    {
        _sut.StartAsync().GetAwaiter().GetResult();
    }

    private void ThenItLogsAtLeastOnce()
    {
        // Allow timer to tick at least once
        Task.Delay(50).GetAwaiter().GetResult();
        _messages.Should().Contain(m => m == "hb");
    }

    private void ThenCanBeStoppedAndDisposedTwiceWithoutError()
    {
        _sut.StopAsync().GetAwaiter().GetResult();
        _sut.Dispose();
        // idempotent
        _sut.StopAsync().GetAwaiter().GetResult();
        _sut.Dispose();
    }

}
