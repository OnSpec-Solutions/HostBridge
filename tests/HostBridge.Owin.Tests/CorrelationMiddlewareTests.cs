using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using HostBridge.Abstractions;
using HostBridge.AspNet;
using HostBridge.Core;
using HostBridge.Owin;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Owin;
using Owin;
using Shouldly;
using Xunit;

namespace HostBridge.Owin.Tests;

public class CorrelationMiddlewareTests
{
    [Fact]
    public async Task UseHostBridgeCorrelation_sets_and_clears_accessor()
    {
        // Arrange root services with logging + correlation accessor
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton<ICorrelationAccessor, CorrelationAccessor>();
        var sp = services.BuildServiceProvider();
        AspNetBootstrapper.Initialize(new TestHost(sp));

        var app = new Microsoft.Owin.Builder.AppBuilder();
        RequestScopeMiddleware.UseHostBridgeRequestScope(app);
        CorrelationMiddleware.UseHostBridgeCorrelation(app);

        string? seen = null;
        // Terminal middleware reads the accessor during the request
        app.Use(async (env, next) =>
        {
            var accessor = sp.GetRequiredService<ICorrelationAccessor>();
            seen = accessor.CorrelationId;
            await Task.CompletedTask;
        });

        var appFunc = (Func<IDictionary<string, object>, Task>)app.Build(typeof(Func<IDictionary<string, object>, Task>));

        var env = new Dictionary<string, object>();
        // Provide OWIN request headers
        var headers = new Dictionary<string, string[]>(StringComparer.OrdinalIgnoreCase);
        var cid = Guid.NewGuid().ToString("N");
        headers[Constants.CorrelationHeaderName] = new[] { cid };
        env["owin.RequestHeaders"] = headers;

        // Act
        await appFunc(env);

        // Assert
        seen.ShouldBe(cid);
        sp.GetRequiredService<ICorrelationAccessor>().CorrelationId.ShouldBeNull();
    }

    private sealed class TestHost : ILegacyHost
    {
        public TestHost(IServiceProvider provider) => ServiceProvider = provider;
        public IServiceProvider ServiceProvider { get; }
        public Task StartAsync(System.Threading.CancellationToken ct = default) => Task.CompletedTask;
        public Task StopAsync(System.Threading.CancellationToken ct = default) => Task.CompletedTask;
        public void Dispose() { }
    }
}
