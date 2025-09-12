using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Web;
using System.Web.Http;
using System.Web.Mvc;
using FluentAssertions;
using HostBridge.Abstractions;
using HostBridge.AspNet;
using HostBridge.Core;
using HostBridge.Diagnostics;
using HostBridge.Mvc5;
using HostBridge.Tests.Common.TestHelpers;
using HostBridge.WebApi2;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;
using TestStack.BDDfy;
using Xunit;

namespace HostBridge.Diagnostics.Tests;

public class AspNetChecksTests
{
    private sealed class DummyApiResolver : System.Web.Http.Dependencies.IDependencyResolver
    {
        public System.Web.Http.Dependencies.IDependencyScope BeginScope() => this;
        public object? GetService(Type serviceType) => null;
        public IEnumerable<object> GetServices(Type serviceType) => Enumerable.Empty<object>();
        public void Dispose() { }
    }
    private static void SetRootInitialized()
    {
        var host = new LegacyHostBuilder()
            .ConfigureLogging(_ => { })
            .ConfigureServices((_, s) =>
            {
                s.AddOptions();
                s.AddScoped<object>();
            })
            .Build();
        AspNetBootstrapper.Initialize(host);
    }

    private static void ResetRoot()
    {
        // Reset RootServices via reflection (internal helper may not be visible here)
        var t = typeof(AspNetBootstrapper);
        var f = t.GetField("<RootServices>k__BackingField", System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic);
        f!.SetValue(null, null);
    }

    private static IReadOnlyList<DiagnosticResult> RunChecks()
        => new HostBridgeVerifier().Add(AspNetChecks.VerifyAspNet).Run();

    [Fact]
    public void Given_no_root_initialized_When_VerifyAspNet_Then_reports_HB_ASP_001_and_info_if_no_HttpContext()
    {
        ResetRoot();
        HttpContext.Current = null;

        var results = RunChecks();

        results.Should().Contain(r => r.ToString().Contains("HB-ASP-001"));
        results.Should().Contain(r => r.ToString().Contains("HB-ASP-000"));
    }

    [Fact]
    public void Given_HttpContext_without_scope_When_VerifyAspNet_Then_reports_missing_module_error()
    {
        SetRootInitialized();
        AspNetTestContext.NewRequest();
        // no scope key attached

        var results = RunChecks();

        results.Should().Contain(r => r.ToString().Contains("HB-ASP-002"));
    }

    [Fact]
    public void Given_all_wired_correctly_When_VerifyAspNet_Then_no_critical_or_error_or_warning()
    {
        SetRootInitialized();

        // HttpContext with request scope
        var ctx = AspNetTestContext.NewRequest();
        var scope = AspNetBootstrapper.RootServices!.CreateScope();
        ctx.Items[Constants.ScopeKey] = scope;

        // Set HostBridge resolvers for MVC and WebApi2
        DependencyResolver.SetResolver(new MvcDependencyResolver());
        GlobalConfiguration.Configuration.DependencyResolver = new WebApiDependencyResolver();

        var results = RunChecks();

        // Dispose scope to avoid leaks in subsequent tests
        scope.Dispose();

        var hasBad = results.Any(r => r.Severity == Severity.Critical || r.Severity == Severity.Error || r.Severity == Severity.Warning);
        hasBad.Should().BeFalse();
    }

    [Fact]
    public void Given_non_hostbridge_resolvers_When_VerifyAspNet_Then_warns_for_Mvc_and_WebApi()
    {
        SetRootInitialized();
        AspNetTestContext.NewRequest(AspNetBootstrapper.RootServices!.CreateScope());

        // Set dummy resolvers that are not HostBridge
        DependencyResolver.SetResolver(_ => null, _ => Enumerable.Empty<object>());
        GlobalConfiguration.Configuration.DependencyResolver = new DummyApiResolver();

        var results = RunChecks();

        results.Should().Contain(r => r.ToString().Contains("HB-MVC-001"));
        results.Should().Contain(r => r.ToString().Contains("HB-WEBAPI2-001"));
    }
}
