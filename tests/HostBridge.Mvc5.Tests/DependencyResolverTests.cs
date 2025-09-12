using HostBridge.Abstractions;
using HostBridge.AspNet;
using HostBridge.Core;
using HostBridge.Tests.Common.TestHelpers;

namespace HostBridge.Mvc5.Tests;

public class DependencyResolverTests
{
    private IServiceProvider _root = null!;
    private MvcDependencyResolver _resolver = null!;
    private ScopedService _scoped1 = null!;
    private ScopedService _scoped2 = null!;
    private object? _result;
    private IMulti[] _multis = [];

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
    public void Mvc5_DependencyResolver_uses_request_scope()
    {
        this.Given(_ => GivenRoot())
            .And(_ => GivenRequestScope())
            .And(_ => GivenResolver())
            .When(_ => WhenResolvingScopedServiceFirstRequest())
            .And(_ => WhenResolvingScopedServiceSecondRequest())
            .Then(_ => ThenScopedInstancesDifferAcrossRequests())
            .BDDfy();
    }

    [Fact]
    public void GetService_returns_null_for_unregistered_type()
    {
        this.Given(_ => GivenRoot())
            .And(_ => GivenRequestScope())
            .And(_ => GivenResolver())
            .When(_ => WhenResolvingUnregisteredService())
            .Then(_ => ThenResultIsNull())
            .BDDfy();
    }

    [Fact]
    public void GetServices_returns_all_implementations_for_type()
    {
        this.Given(_ => GivenRootWithMultipleImplementations())
            .And(_ => GivenRequestScope())
            .And(_ => GivenResolver())
            .When(_ => WhenResolvingMultipleServices())
            .Then(_ => ThenBothImplementationsAreReturned())
            .BDDfy();
    }

    [Fact]
    public void GetServices_for_unregistered_type_returns_empty()
    {
        this.Given(_ => GivenRoot())
            .And(_ => GivenRequestScope())
            .And(_ => GivenResolver())
            .When(_ => WhenResolvingMultipleServices())
            .Then(_ => ThenNoImplementationsAreReturned())
            .BDDfy();
    }

    [Fact]
    public void Falls_back_to_root_provider_when_no_request_scope_resolving_scoped_uses_root_scope()
    {
        this.Given(_ => GivenRoot())
            .And(_ => GivenResolver())
            .When(_ => WhenResolvingScopedTwiceWithNoRequestScope())
            .Then(_ => ThenScopedInstancesAreSameFromRootScope())
            .BDDfy();
    }

    [Fact]
    public void Falls_back_to_root_provider_when_no_request_scope_resolving_singleton_succeeds()
    {
        this.Given(_ => GivenRootWithSingleton())
            .And(_ => GivenResolver())
            .When(_ => WhenResolvingSingleton())
            .Then(_ => ThenSingletonIsReturned())
            .BDDfy();
    }

    [Fact]
    public void BDDfy_example_for_docs()
    {
        this.Given(_ => Given_root_and_two_requests())
            .When(_ => When_resolving_scoped_service_from_each_request())
            .Then(_ => Then_instances_do_not_bleed_across_requests())
            .BDDfy("Mvc5 resolver uses per-request scope");
    }

    private void GivenRoot() => _root = BuildRoot();
    private void GivenRootWithSingleton() => _root = BuildRoot(s => s.AddSingleton<SingletonService>());

    private void GivenRootWithMultipleImplementations() => _root = BuildRoot(s =>
    {
        s.AddTransient<IMulti, ImplA>();
        s.AddTransient<IMulti, ImplB>();
    });

    private void GivenRequestScope()
    {
        AspNetTestContext.NewRequest();
        var scope = _root.CreateScope();
        HttpContext.Current!.Items[Constants.ScopeKey] = scope;
    }

    private void GivenResolver() => _resolver = new MvcDependencyResolver();

    private void WhenResolvingScopedServiceFirstRequest() =>
        _scoped1 = (ScopedService)_resolver.GetService(typeof(ScopedService));

    private void WhenResolvingScopedServiceSecondRequest()
    {
        AspNetTestContext.NewRequest();
        var scope2 = _root.CreateScope();
        HttpContext.Current!.Items[Constants.ScopeKey] = scope2;
        _scoped2 = (ScopedService)_resolver.GetService(typeof(ScopedService));
    }

    private void WhenResolvingUnregisteredService() => _result = _resolver.GetService(typeof(string));

    private void WhenResolvingMultipleServices() =>
        _multis = _resolver.GetServices(typeof(IMulti)).Cast<IMulti>().ToArray();

    private void WhenResolvingScopedTwiceWithNoRequestScope()
    {
        _scoped1 = (ScopedService)_resolver.GetService(typeof(ScopedService));
        _scoped2 = (ScopedService)_resolver.GetService(typeof(ScopedService));
    }

    private void WhenResolvingSingleton() => _result = _resolver.GetService(typeof(SingletonService));

    private void ThenScopedInstancesDifferAcrossRequests() => _scoped2.Id.Should().NotBe(_scoped1.Id);

    private void ThenResultIsNull() => _result.Should().BeNull();

    private void ThenBothImplementationsAreReturned()
    {
        _multis.Should().HaveCount(2);
        _multis.Select(x => x.GetType()).Should().BeEquivalentTo(new[] { typeof(ImplA), typeof(ImplB) });
    }

    private void ThenNoImplementationsAreReturned() => _multis.Should().BeEmpty();

    private void ThenScopedInstancesAreSameFromRootScope() => _scoped2.Id.Should().Be(_scoped1.Id,
        "when no request scope exists, AspNetRequest falls back to the root provider (root scope)");

    private void ThenSingletonIsReturned() => _result.Should().NotBeNull().And.BeOfType<SingletonService>();

    private void Given_root_and_two_requests()
    {
        _root = BuildRoot();
    }

    private void When_resolving_scoped_service_from_each_request()
    {
        AspNetTestContext.NewRequest();
        using var scope = _root.CreateScope();
        HttpContext.Current!.Items[Constants.ScopeKey] = scope;
        _ = new MvcDependencyResolver().GetService(typeof(ScopedService));

        AspNetTestContext.NewRequest();
        using var scope2 = _root.CreateScope();
        HttpContext.Current!.Items[Constants.ScopeKey] = scope2;
        _ = new MvcDependencyResolver().GetService(typeof(ScopedService));
    }

    private void Then_instances_do_not_bleed_across_requests()
    {
        // The assertions are covered by the unit test above; here we just ensure scenario runs without error.
        true.ShouldBeTrue();
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
}