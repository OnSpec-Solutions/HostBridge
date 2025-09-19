using HostBridge.Abstractions;
using HostBridge.AspNet;
using HostBridge.Core;
using HostBridge.Tests.Common.Fakes;
using HostBridge.Tests.Common.TestHelpers;

namespace HostBridge.Mvc5.Tests;

public class CorrelationTests
{
    [Fact]
    public void Given_header_present_When_OnBegin_OnEnd_Then_accessor_set_and_cleared()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton<ICorrelationAccessor, CorrelationAccessor>();
        var sp = services.BuildServiceProvider();
        AspNetBootstrapper.Initialize(new FakeLegacyHost(sp));

        var ctx = AspNetTestContext.NewRequest();
        var header = Constants.CorrelationHeaderName;
        var cid = Guid.NewGuid().ToString("N");
        ctx.Request.Headers.Add(header, cid);

        var accessor = sp.GetRequiredService<ICorrelationAccessor>();

        CorrelationHttpModule.OnBegin();
        accessor.CorrelationId.Should().Be(cid);
        CorrelationHttpModule.OnEnd();
        accessor.CorrelationId.Should().BeNull();
    }
}
