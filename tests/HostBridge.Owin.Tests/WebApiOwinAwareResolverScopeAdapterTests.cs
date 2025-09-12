using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Web;
using HostBridge.Abstractions;
using HostBridge.AspNet;
using HostBridge.Core;
using HostBridge.Owin;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;
using Xunit;

namespace HostBridge.Owin.Tests;

public class WebApiOwinAwareResolverScopeAdapterTests
{
    private static IServiceProvider BuildRoot(Action<IServiceCollection>? configure = null)
    {
        var host = new LegacyHostBuilder()
            .ConfigureLogging(_ => { })
            .ConfigureServices((_, s) =>
            {
                s.AddOptions();
                s.AddScoped<ScopedDep>();
                s.AddTransient<IMulti, A>();
                s.AddTransient<IMulti, B>();
                configure?.Invoke(s);
            })
            .Build();
        AspNetBootstrapper.Initialize(host);
        return AspNetBootstrapper.RootServices!;
    }

    private static void NewHttpContext() => HttpContext.Current = new HttpContext(new HttpRequest("t","http://localhost/",""), new HttpResponse(new StringWriter()));

    [Fact]
    public void BeginScope_uses_owin_scope_and_GetService_GetServices_work_and_Dispose_is_noop()
    {
        var root = BuildRoot();
        NewHttpContext();

        var env = new Dictionary<string, object>();
        using var scope = root.CreateScope();
        env[Constants.ScopeKey] = scope;
        HttpContext.Current!.Items["owin.Environment"] = env;

        var sut = new WebApiOwinAwareResolver();
        using var adapter = sut.BeginScope();

        var dep = adapter.GetService(typeof(ScopedDep)) as ScopedDep;
        dep.ShouldNotBeNull();

        var multis = adapter.GetServices(typeof(IMulti)).OfType<IMulti>().ToArray();
        multis.Length.ShouldBe(2);
        multis.Select(m => m.GetType()).ShouldBe(new[] { typeof(A), typeof(B) }, ignoreOrder: true);

        // Dispose is a no-op; nothing to assert beyond reaching here without exception
    }

    [Fact]
    public void BeginScope_falls_back_to_AspNet_scope_when_no_owin_env()
    {
        var root = BuildRoot();
        NewHttpContext();
        using var asp = root.CreateScope();
        HttpContext.Current!.Items[Constants.ScopeKey] = asp;

        var sut = new WebApiOwinAwareResolver();
        using var adapter = sut.BeginScope();
        adapter.GetService(typeof(ScopedDep)).ShouldNotBeNull();
    }

    [Fact]
    public void BeginScope_falls_back_to_root_when_no_scopes_exist()
    {
        BuildRoot();
        NewHttpContext();
        HttpContext.Current!.Items.Clear();

        var sut = new WebApiOwinAwareResolver();
        using var adapter = sut.BeginScope();
        // Root has ScopedDep registered; without a per-request scope, resolution still works from root
        adapter.GetService(typeof(ScopedDep)).ShouldNotBeNull();
    }

    public sealed class ScopedDep { }
    public interface IMulti { }
    public sealed class A : IMulti { }
    public sealed class B : IMulti { }
}
