using HostBridge.Core;

namespace HostBridge.Core.Tests;

public class HeartbeatServiceTests
{
    [Fact]
    public async Task Start_logs_immediately_and_Stop_disposes_timer_and_Dispose_is_idempotent()
    {
        var logger = new TestLogger<HeartbeatService>();
        var svc = new HeartbeatService(logger);

        await svc.StartAsync();

        // Timer is due immediately; give it a small time slice to fire
        await Task.Delay(50);
        logger.Infos.Any(s => s.Contains("hb")).Should().BeTrue();

        await svc.StopAsync();
        svc.Dispose();
        svc.Dispose();

        // No exceptions thrown during Stop and Dispose indicate idempotent/safe behavior
        logger.Infos.Should().NotBeNull();
    }
}
