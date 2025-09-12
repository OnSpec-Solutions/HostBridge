using HostBridge.Abstractions;
using HostBridge.AspNet;
using HostBridge.Core;
using HostBridge.Tests.Common.TestHelpers;

namespace HostBridge.WebApi2.Tests;

public class DependencyResolverTests
{
    private IServiceProvider? _root;
    private WebApiDependencyResolver? _resolver;
    private IDisposable? _requestScope;
    private ScopedService? _s1;
    private ScopedService? _s2;
    private object? _result;
    private IMulti[]? _multiResults;
    private ScopedDisposable? _scopedDisposable;
    private System.Web.Http.Dependencies.IDependencyScope? _adapter;

    private static IServiceProvider BuildRootInternal(Action<IServiceCollection>? configure = null)
    {
        return BuildRoot(configure);
    }

    private void GivenRootDefault()
    {
        _root = BuildRootInternal();
    }

    private void GivenRootWithMulti()
    {
        _root = BuildRootInternal(s =>
        {
            s.AddTransient<IMulti, ImplA>();
            s.AddTransient<IMulti, ImplB>();
        });
    }

    private void GivenRootWithSingleton()
    {
        _root = BuildRootInternal(s => s.AddSingleton<SingletonService>());
    }

    private void GivenRootWithScopedDisposable()
    {
        _root = BuildRootInternal(s =>
        {
            s.AddScoped<ScopedDisposable>();
            s.AddTransient<IMulti, ImplA>();
        });
    }

    private void GivenRequestScope()
    {
        AspNetTestContext.NewRequest();
        _requestScope = _root!.CreateScope();
        HttpContext.Current!.Items[Constants.ScopeKey] = _requestScope;
    }

    private void GivenResolverCreated()
    {
        _resolver = new WebApiDependencyResolver();
    }

    private void WhenResolveScopedServiceAsS1()
    {
        _s1 = (ScopedService)_resolver!.GetService(typeof(ScopedService));
    }

    private void WhenNewRequestAndResolveScopedAsS2()
    {
        AspNetTestContext.NewRequest();
        using var scope2 = _root!.CreateScope();
        HttpContext.Current!.Items[Constants.ScopeKey] = scope2;
        _s2 = (ScopedService)_resolver!.GetService(typeof(ScopedService));
    }

    private void WhenGetServiceString()
    {
        _result = _resolver!.GetService(typeof(string));
    }

    private void WhenGetServicesIMulti()
    {
        _multiResults = _resolver!.GetServices(typeof(IMulti)).Cast<IMulti>().ToArray();
    }

    private void WhenGetServiceScopedTwice()
    {
        _s1 = (ScopedService)_resolver!.GetService(typeof(ScopedService));
        _s2 = (ScopedService)_resolver!.GetService(typeof(ScopedService));
    }

    private void WhenGetServiceSingleton()
    {
        _result = _resolver!.GetService(typeof(SingletonService));
    }

    private void WhenBeginScopeAdapter()
    {
        _adapter = _resolver!.BeginScope();
    }

    private void WhenGetScopedDisposableViaAdapter()
    {
        _scopedDisposable = (ScopedDisposable)_adapter!.GetService(typeof(ScopedDisposable));
    }

    private void WhenGetServicesIMultiViaAdapter()
    {
        _multiResults = _adapter!.GetServices(typeof(IMulti)).Cast<IMulti>().ToArray();
    }

    private void WhenDisposeAdapter()
    {
        _adapter!.Dispose();
    }

    private void WhenDisposeRequestScope()
    {
        _requestScope!.Dispose();
    }

    private void WhenDisposeResolver()
    {
        _resolver!.Dispose();
    }

    private void ThenS2DifferentFromS1()
    {
        _s2!.Id.Should().NotBe(_s1!.Id);
    }

    private void ThenResultIsNull()
    {
        _result.Should().BeNull();
    }

    private void ThenMultiResultsCountIs(int expected)
    {
        _multiResults!.Length.Should().Be(expected);
    }

    private void ThenMultiResultsTypesAre(params Type[] types)
    {
        _multiResults!.Select(x => x.GetType()).Should().BeEquivalentTo(types);
    }

    private void ThenScopedIdsAreEqualBecauseRootFallback()
    {
        _s2!.Id.Should().Be(_s1!.Id,
            "when no request scope exists, AspNetRequest falls back to the root provider (root scope)");
    }

    private void ThenResultNotNullAndOfType<T>()
    {
        _result.Should().NotBeNull().And.BeOfType<T>();
    }

    private void ThenScopedNotDisposed()
    {
        _scopedDisposable!.Disposed.Should().BeFalse();
    }

    private void ThenScopedDisposed()
    {
        _scopedDisposable!.Disposed.Should().BeTrue();
    }

    private void ThenScopedDisposableNotNull()
    {
        _scopedDisposable.Should().NotBeNull();
    }

    private static IServiceProvider BuildRoot(Action<IServiceCollection>? configure = null)
    {
        var host = new LegacyHostBuilder()
            .ConfigureLogging(_ =>
            {
                /* ensure ILogger<T> is available */
            })
            .ConfigureServices((_, s) =>
            {
                s.AddOptions();
                s.AddScoped<ScopedService>();
                configure?.Invoke(s);
            })
            .Build();
        AspNetBootstrapper.Initialize(host);
        return AspNetBootstrapper.RootServices!;
    }

    [Fact]
    public void WebApi2_DependencyResolver_uses_request_scope()
    {
        this.Given(_ => GivenRootDefault())
            .And(_ => GivenRequestScope())
            .And(_ => GivenResolverCreated())
            .When(_ => WhenResolveScopedServiceAsS1())
            .When(_ => WhenNewRequestAndResolveScopedAsS2())
            .Then(_ => ThenS2DifferentFromS1())
            .BDDfy();
    }

    [Fact]
    public void GetService_returns_null_for_unregistered_type()
    {
        this.Given(_ => GivenRootDefault())
            .And(_ => GivenRequestScope())
            .And(_ => GivenResolverCreated())
            .When(_ => WhenGetServiceString())
            .Then(_ => ThenResultIsNull())
            .BDDfy();
    }

    [Fact]
    public void GetServices_returns_all_implementations_for_type()
    {
        this.Given(_ => GivenRootWithMulti())
            .And(_ => GivenRequestScope())
            .And(_ => GivenResolverCreated())
            .When(_ => WhenGetServicesIMulti())
            .Then(_ => ThenMultiResultsCountIs(2))
            .Then(_ => ThenMultiResultsTypesAre(typeof(ImplA), typeof(ImplB)))
            .BDDfy();
    }

    [Fact]
    public void GetServices_for_unregistered_type_returns_empty()
    {
        this.Given(_ => GivenRootDefault())
            .And(_ => GivenRequestScope())
            .And(_ => GivenResolverCreated())
            .When(_ => WhenGetServicesIMulti())
            .Then(_ => ThenMultiResultsCountIs(0))
            .BDDfy();
    }

    [Fact]
    public void Falls_back_to_root_provider_when_no_request_scope_resolving_scoped_uses_root_scope()
    {
        this.Given(_ => GivenRootDefault())
            .And(_ => GivenResolverCreated())
            .When(_ => WhenGetServiceScopedTwice())
            .Then(_ => ThenScopedIdsAreEqualBecauseRootFallback())
            .BDDfy();
    }

    [Fact]
    public void Falls_back_to_root_provider_when_no_request_scope_resolving_singleton_succeeds()
    {
        this.Given(_ => GivenRootWithSingleton())
            .And(_ => GivenResolverCreated())
            .When(_ => WhenGetServiceSingleton())
            .Then(_ => ThenResultNotNullAndOfType<SingletonService>())
            .BDDfy();
    }

    [Fact]
    public void ScopeAdapter_GetService_and_GetServices_work_and_dispose_is_noop()
    {
        this.Given(_ => GivenRootWithScopedDisposable())
            .And(_ => GivenRequestScope())
            .And(_ => GivenResolverCreated())
            .When(_ => WhenBeginScopeAdapter())
            .When(_ => WhenGetScopedDisposableViaAdapter())
            .Then(_ => ThenScopedDisposableNotNull())
            .When(_ => WhenGetServicesIMultiViaAdapter())
            .Then(_ => ThenMultiResultsCountIs(1))
            .When(_ => WhenDisposeAdapter())
            .Then(_ => ThenScopedNotDisposed())
            .When(_ => WhenDisposeRequestScope())
            .Then(_ => ThenScopedDisposed())
            .When(_ => WhenDisposeResolver())
            .BDDfy();
    }

    public sealed class ScopedService
    {
        public Guid Id { get; } = Guid.NewGuid();
    }

    public sealed class SingletonService
    {
    }

    public interface IMulti
    {
    }

    public sealed class ImplA : IMulti
    {
    }

    public sealed class ImplB : IMulti
    {
    }

    public sealed class ScopedDisposable : IDisposable
    {
        public bool Disposed { get; private set; }
        public void Dispose() => Disposed = true;
    }
}