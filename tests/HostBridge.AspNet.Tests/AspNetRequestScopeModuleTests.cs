using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Web;
using FluentAssertions;
using HostBridge.Abstractions;
using HostBridge.AspNet;
using HostBridge.Core;
using HostBridge.Tests.Common.TestHelpers;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;
using TestStack.BDDfy;
using Xunit;

namespace HostBridge.AspNet.Tests;

public class AspNetRequestScopeModuleTests
{
    private static void Invoke(string methodName)
    {
        var mi = typeof(AspNetRequestScopeModule)
            .GetMethod(methodName, BindingFlags.Static | BindingFlags.NonPublic);
        mi.ShouldNotBeNull();
        mi!.Invoke(null, null);
    }

    private static IServiceProvider BuildRoot(Action<IServiceCollection>? configure = null)
    {
        var host = new LegacyHostBuilder()
            .ConfigureLogging(_ => { })
            .ConfigureServices((_, s) =>
            {
                s.AddOptions();
                s.AddSingleton<InjectedDep>();
                s.AddScoped<ScopedCounter>();
                configure?.Invoke(s);
            })
            .Build();
        AspNetBootstrapper.Initialize(host);
        return AspNetBootstrapper.RootServices!;
    }

    [Fact]
    public void OnBegin_without_initialization_throws()
    {
#if DEBUG
        AspNetBootstrapper._ResetForTests();
#endif
        AspNetTestContext.NewRequest();
        Action act = () => Invoke("OnBegin");
        act.Should().Throw<TargetInvocationException>()
            .WithInnerException<InvalidOperationException>();
    }

    [Fact]
    public void Module_creates_and_disposes_scope_per_request_and_injects_properties()
    {
        BuildRoot();
        var ctx = AspNetTestContext.NewRequest();

        // Before begin: no scope
        ctx.Items.Contains(Constants.ScopeKey).ShouldBeFalse();

        Invoke("OnBegin");

        // After begin: scope exists
        ctx.Items[Constants.ScopeKey].ShouldNotBeNull();
        ctx.Items[Constants.ScopeKey].ShouldBeAssignableTo<IServiceScope>();

        // Simulate a WebForms page with [FromServices] property
        var page = new TestPage();
        ctx.Handler = page; // set current handler

        Invoke("OnPreRequestHandlerExecute");

        page.Injected.ShouldNotBeNull();
        page.Injected!.WasInjected.Should().BeTrue();

        // Grab scoped service to ensure disposal happens
        var scope = (IServiceScope)ctx.Items[Constants.ScopeKey]!;
        var sp = scope.ServiceProvider;
        var counter = sp.GetRequiredService<ScopedCounter>();
        counter.ShouldNotBeNull();
        counter.DisposeCount.Should().Be(0);

        Invoke("OnEnd");

        // Scope removed and disposed
        ctx.Items.Contains(Constants.ScopeKey).ShouldBeFalse();
        counter.DisposeCount.Should().Be(1);
    }

    private sealed class InjectedDep
    {
        public bool WasInjected { get; set; } = true;
    }

    private sealed class ScopedCounter : IDisposable
    {
        public int DisposeCount;
        public void Dispose() => Interlocked.Increment(ref DisposeCount);
    }

    private sealed class TestPage : System.Web.UI.Page
    {
        [FromServices]
        public InjectedDep? Injected { get; set; }
    }
}
