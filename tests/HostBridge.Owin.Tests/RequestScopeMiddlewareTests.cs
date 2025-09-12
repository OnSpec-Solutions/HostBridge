using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Web;
using Owin;
using HostBridge.Abstractions;
using HostBridge.AspNet;
using HostBridge.Owin;
using Microsoft.Owin;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;
using Xunit;

namespace HostBridge.Owin.Tests;

public class RequestScopeMiddlewareTests
{
    [Fact]
    public async Task UseHostBridgeRequestScope_stores_and_clears_scope_and_disposes_it_on_normal_flow()
    {
        var provider = new TestProvider();
        AspNetBootstrapper.Initialize(new TestHost(provider));

        var app = new Microsoft.Owin.Builder.AppBuilder();
        RequestScopeMiddleware.UseHostBridgeRequestScope(app);

        // Add a terminal middleware that does some async work and completes
        app.Use(async (ctx, next) =>
        {
            await Task.Delay(1);
        });

        var appFunc = (Func<IDictionary<string, object>, Task>)app.Build(typeof(Func<IDictionary<string, object>, Task>));
        var env = new Dictionary<string, object>();

        await appFunc(env);

        env.ContainsKey(Constants.ScopeKey).ShouldBeTrue();
        env[Constants.ScopeKey].ShouldBeNull();
        // Scope disposal semantics verified in the concurrency test where multiple scopes are created.
    }

    [Fact]
    public async Task UseHostBridgeRequestScope_supports_concurrent_requests_without_scope_bleed()
    {
        var provider = new TestProvider();
        AspNetBootstrapper.Initialize(new TestHost(provider));

        var app = new Microsoft.Owin.Builder.AppBuilder();
        RequestScopeMiddleware.UseHostBridgeRequestScope(app);

        // Add a terminal that awaits a small delay ensuring scope exists during execution
        app.Use(async (ctx, next) =>
        {
            await Task.Delay(5);
        });

        var appFunc = (Func<IDictionary<string, object>, Task>)app.Build(typeof(Func<IDictionary<string, object>, Task>));

        const int n = 25;
        var envs = Enumerable.Range(0, n).Select(_ => new Dictionary<string, object>()).ToArray();

        await Task.WhenAll(envs.Select(env => appFunc(env)));

        foreach (var env in envs)
        {
            env.ContainsKey(Constants.ScopeKey).ShouldBeTrue();
            env[Constants.ScopeKey].ShouldBeNull();
        }

        provider.CreatedScopes.Count.ShouldBe(n);
        provider.CreatedScopes.All(s => s.Disposed).ShouldBeTrue();
    }

    private sealed class TestHost : ILegacyHost
    {
        public TestHost(IServiceProvider provider) => ServiceProvider = provider;
        public IServiceProvider ServiceProvider { get; }
        public Task StartAsync(System.Threading.CancellationToken ct = default) => Task.CompletedTask;
        public Task StopAsync(System.Threading.CancellationToken ct = default) => Task.CompletedTask;
        public void Dispose() { }
    }

    private sealed class TestProvider : IServiceProvider, IServiceScopeFactory
    {
        public List<TestScope> CreatedScopes { get; } = new();
        public object? GetService(Type serviceType)
        {
            if (serviceType == typeof(IServiceScopeFactory)) return this;
            return null;
        }
        public IServiceScope CreateScope()
        {
            var s = new TestScope(this);
            CreatedScopes.Add(s);
            return s;
        }
    }

    private sealed class TestScope : IServiceScope
    {
        private readonly TestProvider _owner;
        public TestScope(TestProvider owner) { _owner = owner; ServiceProvider = owner; }
        public IServiceProvider ServiceProvider { get; }
        public bool Disposed { get; private set; }
        public void Dispose()
        {
            Disposed = true; // idempotent for test purposes
        }
    }
}
