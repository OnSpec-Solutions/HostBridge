using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Web;
using FluentAssertions;
using HostBridge.Abstractions;
using HostBridge.AspNet;
using HostBridge.Owin;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;
using Xunit;

namespace HostBridge.Owin.Tests;

public class WebApiOwinAwareResolverTests
{
    private readonly WebApiOwinAwareResolver _sut = new();

    private static void SetHttpContext()
    {
        // Minimal HttpContext to allow using HttpContext.Current.Items
        var request = new HttpRequest("test.html", "http://localhost/test.html", string.Empty);
        var response = new HttpResponse(new StringWriter());
        HttpContext.Current = new HttpContext(request, response);
    }

    private static void ClearHttpContext()
    {
        HttpContext.Current = null;
    }

    [Fact]
    public void Given_OwinEnvScope_When_Resolving_Service_Then_Uses_Owin_Scope()
    {
        GivenRootInitialized();
        GivenHttpContextWithOwinScope(out var provider);
        ThenResolutionUsesProvider(provider);
    }

    [Fact]
    public void Given_AspNetScope_When_Resolving_Service_Then_Uses_AspNet_Scope()
    {
        GivenRootInitialized();
        GivenHttpContextWithAspNetScope(out var provider);
        ThenResolutionUsesProvider(provider);
    }

    [Fact]
    public void Given_NoScopes_When_Resolving_Service_Then_Falls_Back_To_Root()
    {
        GivenRootInitialized(out var provider);
        GivenCleanHttpContext();
        ThenResolutionUsesProvider(provider);
    }

    private static void GivenRootInitialized() => GivenRootInitialized(out _);

    private static void GivenRootInitialized(out IServiceProvider provider)
    {
        // Provide a provider that returns a unique value for typeof(string)
        provider = new TestProvider("root");
        AspNetBootstrapper.Initialize(new TestHost(provider));
    }

    private void GivenHttpContextWithOwinScope(out IServiceProvider provider)
    {
        SetHttpContext();
        var env = new Dictionary<string, object>();
        var scopeProvider = new TestProvider("owin");
        var scope = new TestScope(scopeProvider);
        env[Constants.ScopeKey] = scope;
        HttpContext.Current!.Items["owin.Environment"] = env;
        provider = scopeProvider;
    }

    private void GivenHttpContextWithAspNetScope(out IServiceProvider provider)
    {
        SetHttpContext();
        var scopeProvider = new TestProvider("aspnet");
        var scope = new TestScope(scopeProvider);
        HttpContext.Current!.Items[Constants.ScopeKey] = scope;
        provider = scopeProvider;
    }

    private static void GivenCleanHttpContext()
    {
        SetHttpContext();
        HttpContext.Current!.Items.Clear();
    }

    private void ThenResolutionUsesProvider(IServiceProvider expected)
    {
        var result = _sut.GetService(typeof(string));
        result.ShouldNotBeNull();
        result.ShouldBeOfType<string>().ShouldBe(((TestProvider)expected).Name);
    }

    private sealed class TestHost : ILegacyHost
    {
        public TestHost(IServiceProvider provider) => ServiceProvider = provider;
        public IServiceProvider ServiceProvider { get; }
        public Task StartAsync(CancellationToken ct = default) => Task.CompletedTask;
        public Task StopAsync(CancellationToken ct = default) => Task.CompletedTask;
        public void Dispose() { }
    }

    private sealed class TestProvider : IServiceProvider, IServiceScopeFactory
    {
        public string Name { get; }
        public TestProvider(string name) => Name = name;
        public object? GetService(Type serviceType)
        {
            if (serviceType == typeof(IServiceScopeFactory)) return this;
            if (serviceType == typeof(string)) return Name;
            return null;
        }
        public IServiceScope CreateScope() => new TestScope(this);
    }

    private sealed class TestScope : IServiceScope
    {
        public IServiceProvider ServiceProvider { get; }
        public bool Disposed { get; private set; }
        public TestScope(IServiceProvider provider) => ServiceProvider = provider;
        public void Dispose() => Disposed = true;
    }
}
