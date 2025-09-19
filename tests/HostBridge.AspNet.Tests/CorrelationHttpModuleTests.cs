using HostBridge.Abstractions;
using HostBridge.AspNet;
using HostBridge.Core;
using HostBridge.Tests.Common.Fakes;
using HostBridge.Tests.Common.TestHelpers;
using Microsoft.Extensions.Logging;

namespace HostBridge.AspNet.Tests;

public class CorrelationHttpModuleTests
{
    [Fact]
    public void Given_header_present_When_Begin_and_End_Then_accessor_set_and_cleared()
    {
        // Arrange root services with logging + correlation accessor
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

        // Act
        CorrelationHttpModule.OnBegin();
        var during = accessor.CorrelationId;
        CorrelationHttpModule.OnEnd();
        var after = accessor.CorrelationId;

        // Assert
        during.Should().Be(cid);
        after.Should().BeNull();
    }
}
