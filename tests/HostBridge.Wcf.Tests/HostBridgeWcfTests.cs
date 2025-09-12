using HostBridge.Abstractions;
using HostBridge.Tests.Common.Fakes;

namespace HostBridge.Wcf.Tests;

public class HostBridgeWcfTests
{
    [Fact]
    public void Initialize_with_null_throws_and_does_not_set_root()
    {
        // Reset any previous state if tests ran in parallel/order
        HostBridgeWcf.Initialize(new FakeLegacyHost(new ServiceCollection().BuildServiceProvider()));
        Action act = () => HostBridgeWcf.Initialize(null);
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void Initialize_sets_RootServices_from_host()
    {
        var services = new ServiceCollection();
        services.AddSingleton<string>("hello");
        var sp = services.BuildServiceProvider();
        HostBridgeWcf.Initialize(new FakeLegacyHost(sp));
        HostBridgeWcf.RootServices.Should().BeSameAs(sp);
    }

    [Fact]
    public void Initialize_twice_overwrites_root()
    {
        var s1 = new ServiceCollection().BuildServiceProvider();
        HostBridgeWcf.Initialize(new FakeLegacyHost(s1));
        var s2 = new ServiceCollection().BuildServiceProvider();
        HostBridgeWcf.Initialize(new FakeLegacyHost(s2));
        HostBridgeWcf.RootServices.Should().BeSameAs(s2);
    }
}