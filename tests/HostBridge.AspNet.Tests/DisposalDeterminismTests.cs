using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using System.Web;
using FluentAssertions;
using HostBridge.Abstractions;
using HostBridge.AspNet;
using HostBridge.Core;
using Shouldly;
using TestStack.BDDfy;
using Xunit;

namespace HostBridge.AspNet.Tests;

public class DisposalDeterminismTests
{
    private IServiceProvider _root = null!;
    private ScopedDisposable _scopedA = null!, _scopedB = null!;
    private TransientDisposable _transientA = null!, _transientB = null!;
    private SingletonDisposable _singletonA = null!, _singletonB = null!;

    private static IServiceProvider BuildRoot()
    {
        var host = new LegacyHostBuilder()
            .ConfigureLogging(_ => { /* ensure ILogger<T> available */ })
            .ConfigureServices((_, s) =>
            {
                s.AddOptions();
                s.AddSingleton<SingletonDisposable>();
                s.AddScoped<ScopedDisposable>();
                s.AddTransient<TransientDisposable>();
            })
            .Build();

        AspNetBootstrapper.Initialize(host);
        return AspNetBootstrapper.RootServices!;
    }

    private void InitializeRoot() => _root = BuildRoot();

    private void ResolveFirstRequest() => ResolveForOneRequest(out _singletonA, out _scopedA, out _transientA);

    private void ResolveSecondRequest() => ResolveForOneRequest(out _singletonB, out _scopedB, out _transientB);

    [Fact]
    public void Given_two_requests_When_resolving_services_Then_scoped_and_transient_disposed_once_per_request_and_singleton_not_disposed()
    {
        this.Given(_ => InitializeRoot())
            .When(_ => ResolveFirstRequest())
            .And(_ => ResolveSecondRequest())
            .Then(_ => AssertDisposalInvariant())
            .BDDfy();
    }

    [Fact]
    public async Task Given_parallel_requests_When_resolving_services_Then_no_bleed_and_disposed_once_each()
    {
        _root = BuildRoot();
        var ids = new ConcurrentBag<Guid>();
        var start = new ManualResetEventSlim(false);

        async Task RunOne()
        {
            AspNetTestContext.NewRequest();
            using var scope = _root.CreateScope();
            HttpContext.Current!.Items[Constants.ScopeKey] = scope;
            start.Wait();
            var sp = AspNetRequest.RequestServices;
            var scoped = sp.GetRequiredService<ScopedDisposable>();
            ids.Add(scoped.Id);
            _ = sp.GetRequiredService<TransientDisposable>();
        }

        var t1 = Task.Run(RunOne);
        var t2 = Task.Run(RunOne);
        start.Set();
        await Task.WhenAll(t1, t2);

        DiAssertions.ShouldHaveDistinctIds(ids, 2);
    }

    private static void ResolveForOneRequest(out SingletonDisposable singleton, out ScopedDisposable scoped, out TransientDisposable transient)
    {
        AspNetTestContext.NewRequest();
        using var scope = AspNetBootstrapper.RootServices!.CreateScope();
        HttpContext.Current!.Items[Constants.ScopeKey] = scope;

        var sp = AspNetRequest.RequestServices;
        singleton = sp.GetRequiredService<SingletonDisposable>();
        scoped = sp.GetRequiredService<ScopedDisposable>();
        transient = sp.GetRequiredService<TransientDisposable>();
    }

    private void AssertDisposalInvariant()
    {
        _scopedA.Should().NotBeNull();
        _scopedB.Should().NotBeNull();
        _transientA.Should().NotBeNull();
        _transientB.Should().NotBeNull();
        _singletonA.Should().BeSameAs(_singletonB);

        _scopedA.DisposeCount.Should().Be(1);
        _scopedB.DisposeCount.Should().Be(1);

        _transientA.DisposeCount.Should().Be(1);
        _transientB.DisposeCount.Should().Be(1);

        _singletonA.DisposeCount.Should().Be(0);
    }

    public sealed class SingletonDisposable : IDisposable
    {
        public int DisposeCount;
        public void Dispose() => Interlocked.Increment(ref DisposeCount);
    }

    public sealed class ScopedDisposable : IDisposable
    {
        public Guid Id { get; } = Guid.NewGuid();
        public int DisposeCount;
        public void Dispose() => Interlocked.Increment(ref DisposeCount);
    }

    public sealed class TransientDisposable : IDisposable
    {
        public Guid Id { get; } = Guid.NewGuid();
        public int DisposeCount;
        public void Dispose() => Interlocked.Increment(ref DisposeCount);
    }
}
