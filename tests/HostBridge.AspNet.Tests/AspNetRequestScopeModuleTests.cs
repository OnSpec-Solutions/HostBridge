using HostBridge.Abstractions;
using HostBridge.Tests.Common.Fakes;

namespace HostBridge.AspNet.Tests;

public class AspNetRequestScopeModuleTests
{
    private IServiceProvider _root = null!;

    [Fact]
    public void Given_not_initialized_When_BeginRequest_Then_throws()
    {
        // Arrange: ensure no root
        AspNetBootstrapper._ResetForTests();
        AspNetTestContext.NewRequest();

        // Act + Assert
        Assert.Throws<InvalidOperationException>(() => AspNetRequestScopeModule.OnBegin());
    }

    [Fact]
    public void Given_initialized_When_request_lifecycle_runs_Then_scope_is_created_injected_and_disposed()
    {
        // Arrange root with a scoped disposable that records disposal
        var disposed = false;
        var services = new ServiceCollection();
        services.AddScoped(_ => new MyDep(() => disposed = true));
        var host = new FakeLegacyHost(services.BuildServiceProvider());
        AspNetBootstrapper.Initialize(host);

        var ctx = AspNetTestContext.NewRequest();

        // BeginRequest should create a scope and store it
        AspNetRequestScopeModule.OnBegin();
        ctx.Items.Contains(Constants.ScopeKey).ShouldBeTrue();
        var scope = ctx.Items[Constants.ScopeKey].ShouldBeAssignableTo<IServiceScope>();

        // Set a handler page with injectable and non-injectable properties
        var page = new TestPage();
        HttpContext.Current!.Handler = page;

        // PreRequestHandlerExecute should inject only [FromServices] property
        AspNetRequestScopeModule.OnPreRequestHandlerExecute();
        page.Injected.ShouldNotBeNull();
        page.NotInjected.Should().BeNull();

        // The resolved instance should be the same as what the scope would resolve
        var resolved = page.Injected!;
        var viaScope = scope.ServiceProvider.GetRequiredService<MyDep>();
        ReferenceEquals(resolved, viaScope).Should().BeTrue("injected instance must come from the request scope");

        // EndRequest should dispose scope and remove key
        AspNetRequestScopeModule.OnEnd();
        ctx.Items.Contains(Constants.ScopeKey).ShouldBeFalse();
        disposed.Should().BeTrue();
    }

    private sealed class MyDep(Action onDispose) : IDisposable
    {
        public IServiceProvider? Source { get; set; }
        public void Dispose() => onDispose();
    }

    private sealed class TestPage : System.Web.UI.Page
    {
        [FromServices] public MyDep? Injected { get; set; }
        public object? NotInjected { get; set; }

        protected override void OnPreInit(EventArgs e)
        {
            base.OnPreInit(e);
            // If Injected was set, attach Source for verification
            if (Injected != null)
            {
                Injected.Source = AspNetRequest.RequestServices;
            }
        }
    }

}