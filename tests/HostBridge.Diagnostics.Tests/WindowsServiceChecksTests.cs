using System.Linq;
using FluentAssertions;
using HostBridge.Diagnostics;
using Xunit;

namespace HostBridge.Diagnostics.Tests;

public class WindowsServiceChecksTests
{
    [Fact]
    public void When_running_as_console_then_info_is_reported()
    {
        var results = new HostBridgeVerifier().Add(() => WindowsServiceChecks.VerifyWindowsService(runningAsService: false)).Run();
        results.Should().Contain(r => r.ToString().Contains("HB-SVC-INFO"));
    }

    [Fact]
    public void When_running_as_service_then_no_results_are_reported()
    {
        var results = new HostBridgeVerifier().Add(() => WindowsServiceChecks.VerifyWindowsService(runningAsService: true)).Run();
        results.Any().Should().BeFalse();
    }
}
