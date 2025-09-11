using System;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;

using Shouldly;
using TestStack.BDDfy;
using Xunit;

namespace HostBridge.Abstractions.Tests;

public class ILegacyHostTests
{
    // Fields
    private Type _sutType = null!;
    private PropertyInfo _serviceProviderProp = null!;
    private MethodInfo _startAsync = null!;
    private MethodInfo _stopAsync = null!;

    // Scenario
    [Fact]
    public void Given_ILegacyHost_When_inspecting_contract_Then_it_matches_expected_shape()
    {
        this.Given(_ => GivenTheInterfaceType())
            .When(_ => WhenReadingMembers())
            .Then(_ => ThenTheContractIsAsExpected())
            .BDDfy();
    }

    // Steps and Assertions
    private void GivenTheInterfaceType()
    {
        _sutType = typeof(ILegacyHost);
    }

    private void WhenReadingMembers()
    {
        _serviceProviderProp = _sutType.GetProperty(nameof(ILegacyHost.ServiceProvider))!;
        _startAsync = _sutType.GetMethod(nameof(ILegacyHost.StartAsync))!;
        _stopAsync = _sutType.GetMethod(nameof(ILegacyHost.StopAsync))!;
    }

    private void ThenTheContractIsAsExpected()
    {
        // Inherits IDisposable
        typeof(IDisposable).IsAssignableFrom(_sutType).ShouldBeTrue();

        // ServiceProvider property
        _serviceProviderProp.Should().NotBeNull();
        _serviceProviderProp.CanRead.Should().BeTrue();
        _serviceProviderProp.PropertyType.Should().Be(typeof(IServiceProvider));

        // Async methods with CancellationToken optional/default parameter
        _startAsync.ReturnType.Should().Be(typeof(Task));
        _stopAsync.ReturnType.Should().Be(typeof(Task));

        var startParams = _startAsync.GetParameters();
        var stopParams = _stopAsync.GetParameters();

        startParams.Length.ShouldBe(1);
        stopParams.Length.ShouldBe(1);

        startParams[0].ParameterType.ShouldBe(typeof(CancellationToken));
        stopParams[0].ParameterType.ShouldBe(typeof(CancellationToken));

        (startParams[0].IsOptional || startParams[0].HasDefaultValue).ShouldBeTrue();
        (stopParams[0].IsOptional || stopParams[0].HasDefaultValue).ShouldBeTrue();
    }
}
