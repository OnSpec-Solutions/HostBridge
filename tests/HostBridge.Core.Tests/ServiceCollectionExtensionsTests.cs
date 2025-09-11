using System.Linq;
using FluentAssertions;
using HostBridge.Abstractions;
using HostBridge.Tests.Common.Fakes;
using Microsoft.Extensions.DependencyInjection;
using TestStack.BDDfy;
using Xunit;

namespace HostBridge.Core.Tests;

public class ServiceCollectionExtensionsTests
{
    private IServiceCollection _services = null!;

    [Fact]
    public void Given_ServiceCollection_When_AddHostedService_called_Then_registers_IHostedService_singleton()
    {
        this.Given(_ => GivenServices())
            .When(_ => WhenRegisteringHostedService<DistinctHostedServiceA>())
            .Then(_ => ThenIHostedServiceIsRegisteredOnceWithImplementation<DistinctHostedServiceA>())
            .BDDfy();
    }

    [Fact]
    public void Given_ServiceCollection_When_AddHostedService_called_twice_Then_both_registrations_exist()
    {
        this.Given(_ => GivenServices())
            .When(_ => WhenRegisteringHostedService<DistinctHostedServiceA>())
            .And(_ => WhenRegisteringHostedService<DistinctHostedServiceB>())
            .Then(_ => ThenBothRegistrationsArePresent<DistinctHostedServiceA, DistinctHostedServiceB>())
            .BDDfy();
    }

    private void GivenServices() => _services = new ServiceCollection();

    private void WhenRegisteringHostedService<T>() where T : class, IHostedService
    {
        _services.AddHostedService<T>();
    }

    private void ThenIHostedServiceIsRegisteredOnceWithImplementation<T>() where T : class, IHostedService
    {
        using var sp = _services.BuildServiceProvider();
        var hosted = sp.GetServices<IHostedService>();
        hosted.Should().ContainSingle().Which.Should().BeOfType<T>();
        // singleton semantics
        var a = sp.GetRequiredService<IHostedService>();
        var b = sp.GetRequiredService<IHostedService>();
        a.Should().BeSameAs(b);
    }

    private void ThenBothRegistrationsArePresent<T1, T2>() where T1 : class, IHostedService where T2 : class, IHostedService
    {
        using var sp = _services.BuildServiceProvider();
        var hosted = sp.GetServices<IHostedService>();

        hosted.Should()
            .AllBeAssignableTo<IHostedService>();

        hosted.Select(h => h.GetType()).Should()
            .BeEquivalentTo(new[] { typeof(T1), typeof(T2) });
    }

}
