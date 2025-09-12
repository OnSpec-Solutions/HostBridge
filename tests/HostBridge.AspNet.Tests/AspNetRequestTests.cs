using HostBridge.Abstractions;
using HostBridge.Tests.Common.Fakes;

namespace HostBridge.AspNet.Tests;

public class AspNetRequestTests
{
    private Exception? _ex;
    private IServiceProvider _root = null!;

    [Fact]
    public void Given_not_initialized_When_accessing_RequestServices_Then_throws()
    {
        this.Given(_ => GivenNotInitializedAndNoHttpContext())
            .When(_ => WhenAccessingRequestServicesExpectingFailure())
            .Then(_ => ThenInvalidOperationIsThrown())
            .BDDfy();
    }

    [Fact]
    public void Given_initialized_When_no_HttpContext_Then_returns_root()
    {
        this.Given(_ => GivenInitializedWith(s => s.AddSingleton<string>("a")))
            .When(_ => WhenClearingHttpContext())
            .Then(_ => ThenRequestServicesIsRoot())
            .BDDfy();
    }

    [Fact]
    public void Given_initialized_When_HttpContext_without_scope_Then_returns_root()
    {
        this.Given(_ => GivenInitializedWith(s => s.AddSingleton<string>("a")))
            .When(_ => WhenSettingNewRequestWithoutScope())
            .Then(_ => ThenRequestServicesIsRoot())
            .BDDfy();
    }

    [Fact]
    public void Given_initialized_When_HttpContext_with_scope_Then_returns_scope_provider()
    {
        this.Given(_ => GivenInitializedWith(s => s.AddSingleton<string>("a")))
            .When(_ => WhenSettingNewRequestWithScope())
            .Then(_ => ThenRequestServicesIsScopedProvider())
            .BDDfy();
    }

    private static void GivenNotInitializedAndNoHttpContext()
    {
        AspNetBootstrapper._ResetForTests();
        HttpContext.Current = null;
    }

    private void WhenAccessingRequestServicesExpectingFailure()
    {
        _ex = Assert.Throws<InvalidOperationException>(() => _ = AspNetRequest.RequestServices);
    }

    private void ThenInvalidOperationIsThrown()
    {
        _ex.Should().BeOfType<InvalidOperationException>();
        _ex!.Message.Should().Contain("not initialized");
    }

    private void GivenInitializedWith(Action<IServiceCollection> configure)
    {
        var services = new ServiceCollection();
        configure(services);
        _root = services.BuildServiceProvider();
        AspNetBootstrapper.Initialize(new FakeLegacyHost(_root));
    }

    private static void WhenClearingHttpContext()
    {
        HttpContext.Current = null;
    }

    private void ThenRequestServicesIsRoot()
    {
        AspNetRequest.RequestServices.Should().BeSameAs(_root);
    }

    private void WhenSettingNewRequestWithoutScope()
    {
        AspNetTestContext.NewRequest();
    }

    private IServiceProvider _scopedSp = null!;

    private void WhenSettingNewRequestWithScope()
    {
        var ctx = AspNetTestContext.NewRequest();
        var scope = _root.CreateScope();
        ctx.Items[Constants.ScopeKey] = scope;
        _scopedSp = scope.ServiceProvider;
    }

    private void ThenRequestServicesIsScopedProvider()
    {
        AspNetRequest.RequestServices.Should().BeSameAs(_scopedSp);
    }

}