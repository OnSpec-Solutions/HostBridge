using HostBridge.Abstractions;
using HostBridge.Tests.Common.Fakes;

namespace HostBridge.AspNet.Tests;

public class AspNetBootstrapperTests
{
    private Exception? _ex;
    private IServiceProvider _root = null!;

    [Fact]
    public void Given_null_host_When_initialize_Then_throws()
    {
        this.Given(_ => GivenNotInitialized())
            .When(_ => WhenInitializeWithNull())
            .Then(_ => ThenArgumentNullIsThrown())
            .BDDfy();
    }

    [Fact]
    public void Given_valid_host_When_initialize_Then_root_is_set()
    {
        this.Given(_ => GivenNotInitialized())
            .When(_ => WhenInitializeWithValidHost())
            .Then(_ => ThenRootIsSet())
            .BDDfy();
    }

    private static void GivenNotInitialized()
    {
        // No global reset needed; AspNetBootstrapper.RootServices is set per-process, but tests initialize explicitly.
        // Ensure it's null via reflection in case another test ran previously.
        var t = typeof(AspNetBootstrapper);
        var field = t.GetField("<RootServices>k__BackingField",
            System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic);
        field!.SetValue(null, null);
    }

    private void WhenInitializeWithNull()
    {
        _ex = Assert.Throws<ArgumentNullException>(() => AspNetBootstrapper.Initialize(null));
    }

    private void ThenArgumentNullIsThrown()
    {
        _ex.Should().BeOfType<ArgumentNullException>();
    }

    private void WhenInitializeWithValidHost()
    {
        var services = new ServiceCollection();
        services.AddSingleton<string>("hello");
        _root = services.BuildServiceProvider();
        AspNetBootstrapper.Initialize(new FakeLegacyHost(_root));
    }

    private void ThenRootIsSet()
    {
        AspNetBootstrapper.RootServices.Should().BeSameAs(_root);
    }

}