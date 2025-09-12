using System;
using System.Collections.Generic;
using System.IO;
using System.Web;
using HostBridge.Abstractions;
using HostBridge.AspNet;
using HostBridge.Core;
using HostBridge.Owin;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;
using Xunit;

namespace HostBridge.Owin.Tests;

public class WebApiOwinAwareResolverEdgeTests
{
    private static IServiceProvider BuildRoot()
    {
        var host = new LegacyHostBuilder()
            .ConfigureLogging(_ => { })
            .ConfigureServices((_, s) =>
            {
                s.AddOptions();
                s.AddScoped<ScopedMarker>();
            })
            .Build();
        AspNetBootstrapper.Initialize(host);
        return AspNetBootstrapper.RootServices!;
    }

    [Fact]
    public void Given_OwinEnv_has_wrong_type_for_scopekey_When_resolving_Then_falls_back_to_AspNet_scope()
    {
        var root = BuildRoot();

        // Prepare HttpContext with an invalid OWIN env scope value
        var request = new HttpRequest("test.html", "http://localhost/test.html", string.Empty);
        var response = new HttpResponse(new StringWriter());
        HttpContext.Current = new HttpContext(request, response);

        var env = new Dictionary<string, object>
        {
            // Wrong type (string) instead of IServiceScope
            [Constants.ScopeKey] = "not-a-scope"
        };
        HttpContext.Current!.Items["owin.Environment"] = env;

        // Also attach a valid ASP.NET scope so resolver can fall back to it
        using var aspScope = root.CreateScope();
        HttpContext.Current!.Items[Constants.ScopeKey] = aspScope;

        var sut = new WebApiOwinAwareResolver();
        var resolved = sut.GetService(typeof(ScopedMarker)) as ScopedMarker;
        resolved.ShouldNotBeNull();
    }

    public sealed class ScopedMarker { }
}
