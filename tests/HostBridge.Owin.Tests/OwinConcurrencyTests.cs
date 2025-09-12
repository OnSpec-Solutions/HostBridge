using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using System.Web;
using FluentAssertions;
using HostBridge.Abstractions;
using HostBridge.AspNet;
using HostBridge.Core;
using HostBridge.Owin;
using HostBridge.Tests.Common.Assertions;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;
using System.IO;
using TestStack.BDDfy;
using Xunit;

namespace HostBridge.Owin.Tests;

public class OwinConcurrencyTests
{
    private readonly WebApiOwinAwareResolver _resolver = new();

    private static IServiceProvider BuildRoot()
    {
        var host = new LegacyHostBuilder()
            .ConfigureLogging(_ => { })
            .ConfigureServices((_, s) =>
            {
                s.AddOptions();
                s.AddScoped<ScopedService>();
                s.AddSingleton<SingletonService>();
            })
            .Build();
        AspNetBootstrapper.Initialize(host);
        return AspNetBootstrapper.RootServices!;
    }

    [Fact]
    public async Task Given_parallel_owin_requests_When_resolving_via_resolver_Then_no_scope_bleed_and_singleton_shared()
    {
        var root = BuildRoot();
        var start = new ManualResetEventSlim(false);
        var scopedIds = new ConcurrentBag<Guid>();
        var singletons = new ConcurrentBag<SingletonService>();

        Task RunOne()
        {
            return Task.Run(() =>
            {
                // New HttpContext and OWIN env with its own scope
                var request = new HttpRequest("test", "http://localhost/", string.Empty);
                var response = new HttpResponse(new StringWriter());
                HttpContext.Current = new HttpContext(request, response);

                var env = new Dictionary<string, object>();
                using var scope = root.CreateScope();
                env[Constants.ScopeKey] = scope;
                HttpContext.Current!.Items["owin.Environment"] = env;

                start.Wait();

                var scoped = _resolver.GetService(typeof(ScopedService)) as ScopedService;
                scoped.ShouldNotBeNull();
                scopedIds.Add(scoped!.Id);

                var singleton = _resolver.GetService(typeof(SingletonService)) as SingletonService;
                singleton.ShouldNotBeNull();
                singletons.Add(singleton!);
            });
        }

        var t1 = RunOne();
        var t2 = RunOne();
        start.Set();
        await Task.WhenAll(t1, t2);

        DiAssertions.ShouldHaveDistinctIds(scopedIds, 2);
        DiAssertions.ShouldBeSingleInstance(singletons);
    }

    public sealed class SingletonService { }
    public sealed class ScopedService { public Guid Id { get; } = Guid.NewGuid(); }
}
