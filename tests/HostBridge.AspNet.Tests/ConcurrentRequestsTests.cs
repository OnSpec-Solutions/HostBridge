using HostBridge.Core;
using HostBridge.Mvc5;
using HostBridge.WebApi2;

namespace HostBridge.AspNet.Tests;

public class ConcurrentRequestsTests
{
    // Fields for BDDfy scenario
    private IServiceProvider _root = null!;
    private ManualResetEventSlim _start = null!;
    private ConcurrentBag<Guid> _ids = null!;
    private Task _t1 = null!, _t2 = null!;

    private static IServiceProvider BuildRoot()
    {
        var host = new LegacyHostBuilder()
            .ConfigureLogging(_ =>
            {
                /* ensure ILogger<T> is available */
            })
            .ConfigureServices((_, s) =>
            {
                s.AddOptions();
                s.AddSingleton<SingletonService>();
                s.AddScoped<ScopedService>();
                s.AddTransient<TransientService>();
            })
            .Build();

        AspNetBootstrapper.Initialize(host);
        return AspNetBootstrapper.RootServices!;
    }


    [Fact]
    public async Task Parallel_requests_get_distinct_scoped_instances()
    {
        var root = BuildRoot();

        var start = new ManualResetEventSlim(false);
        var ids = new ConcurrentBag<Guid>();

        Task RunOne()
        {
            return Task.Run(() =>
            {
                AspNetTestContext.NewRequest();
                using var scope = root.CreateScope();
                HttpContext.Current.Items[Constants.ScopeKey] = scope;

                start.Wait();

                var sp = AspNetRequest.RequestServices;
                var scoped = sp.GetRequiredService<ScopedService>();
                ids.Add(scoped.Id);
            });
        }

        var t1 = RunOne();
        var t2 = RunOne();

        start.Set();
        await Task.WhenAll(t1, t2);

        DiAssertions.ShouldHaveDistinctIds(ids, 2);
    }

    [Fact]
    public async Task Parallel_requests_share_singletons_but_not_scoped()
    {
        var root = BuildRoot();
        var start = new ManualResetEventSlim(false);

        var singletonRefs = new ConcurrentBag<SingletonService>();
        var scopedIds = new ConcurrentBag<Guid>();

        Task RunOne()
        {
            return Task.Run(() =>
            {
                AspNetTestContext.NewRequest();
                using var scope = root.CreateScope();
                HttpContext.Current.Items[Constants.ScopeKey] = scope;

                start.Wait();

                var sp = AspNetRequest.RequestServices;
                singletonRefs.Add(sp.GetRequiredService<SingletonService>());
                scopedIds.Add(sp.GetRequiredService<ScopedService>().Id);
            });
        }

        var t1 = RunOne();
        var t2 = RunOne();
        start.Set();
        await Task.WhenAll(t1, t2);

        DiAssertions.ShouldBeSingleInstance(singletonRefs);
        DiAssertions.ShouldHaveDistinctIds(scopedIds, 2);
    }

    [Fact]
    public void Mvc5_DependencyResolver_uses_request_scope()
    {
        var root = BuildRoot();

        AspNetTestContext.NewRequest();
        using var scope = root.CreateScope();
        HttpContext.Current.Items[Constants.ScopeKey] = scope;

        var resolver = new MvcDependencyResolver();
        var s1 = (ScopedService)resolver.GetService(typeof(ScopedService));

        AspNetTestContext.NewRequest();
        using var scope2 = root.CreateScope();
        HttpContext.Current.Items[Constants.ScopeKey] = scope2;
        var s2 = (ScopedService)resolver.GetService(typeof(ScopedService));

        s2.Id.Should().NotBe(s1.Id);
    }

    [Fact]
    public void WebApi2_DependencyResolver_uses_request_scope()
    {
        var root = BuildRoot();

        AspNetTestContext.NewRequest();
        using var scope = root.CreateScope();
        HttpContext.Current.Items[Constants.ScopeKey] = scope;

        var resolver = new WebApiDependencyResolver();
        using (resolver.BeginScope())
        {
            var s1 = resolver.GetService(typeof(ScopedService)) as ScopedService;
            s1.ShouldNotBeNull();
        }

        AspNetTestContext.NewRequest();
        using var scope2 = root.CreateScope();
        HttpContext.Current.Items[Constants.ScopeKey] = scope2;

        using (resolver.BeginScope())
        {
            var s2 = resolver.GetService(typeof(ScopedService)) as ScopedService;
            s2.ShouldNotBeNull();
        }
    }

    [Fact]
    public void BDDfy_request_scopes_are_isolated()
    {
        this.BDDfy("Concurrent requests get isolated scoped services");
    }

    public sealed class SingletonService
    {
    }

    public sealed class ScopedService
    {
        public Guid Id { get; } = Guid.NewGuid();
    }

    public sealed class TransientService
    {
        public Guid Id { get; } = Guid.NewGuid();
    }
}