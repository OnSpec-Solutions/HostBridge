using System;
using System.Collections.Generic;
using System.IO;
using System.Web;
using HostBridge.Abstractions;
using HostBridge.AspNet;
using HostBridge.Owin;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;
using Xunit;

namespace HostBridge.Owin.Tests;

public class WebApiOwinAwareResolverMoreTests
{
    private readonly WebApiOwinAwareResolver _sut = new();

    private static void SetHttpContext()
    {
        var request = new HttpRequest("test.html", "http://localhost/test.html", string.Empty);
        var response = new HttpResponse(new StringWriter());
        HttpContext.Current = new HttpContext(request, response);
    }


    [Fact]
    public void Given_Provider_With_No_Enumerable_Support_GetServices_Returns_Empty()
    {
        // Use a proper DI root with no IDisposable registrations
        var host = new HostBridge.Core.LegacyHostBuilder()
            .ConfigureLogging(_ => { })
            .ConfigureServices((_, s) => s.AddOptions())
            .Build();
        AspNetBootstrapper.Initialize(host);
        SetHttpContext();
        HttpContext.Current!.Items.Clear();

        var items = _sut.GetServices(typeof(IDisposable));
        items.ShouldNotBeNull();
        items.ShouldBeEmpty();
    }

    private sealed class SimpleHost : ILegacyHost
    {
        public SimpleHost(IServiceProvider p) => ServiceProvider = p;
        public IServiceProvider ServiceProvider { get; }
        public void Dispose() { }
        public System.Threading.Tasks.Task StartAsync(System.Threading.CancellationToken ct = default) => System.Threading.Tasks.Task.CompletedTask;
        public System.Threading.Tasks.Task StopAsync(System.Threading.CancellationToken ct = default) => System.Threading.Tasks.Task.CompletedTask;
    }

    private sealed class SimpleProvider : IServiceProvider
    {
        public object? GetService(Type serviceType)
        {
            if (serviceType == typeof(string)) return "ok";
            return null;
        }
    }
}
