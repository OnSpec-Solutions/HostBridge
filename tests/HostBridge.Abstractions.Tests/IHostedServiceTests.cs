using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;

using Shouldly;
using TestStack.BDDfy;
using Xunit;

namespace HostBridge.Abstractions.Tests;

public class IHostedServiceTests
{
    // Fields
    private System.Type _sutType = null!;
    private MethodInfo _startAsync = null!;
    private MethodInfo _stopAsync = null!;

    // Scenario
    [Fact]
    public void Given_IHostedService_When_inspecting_contract_Then_it_exposes_Start_and_Stop_async_methods()
    {
        this.Given(_ => GivenTheInterfaceType())
            .When(_ => WhenReadingItsMethods())
            .Then(_ => ThenMethodsHaveExpectedSignatures())
            .BDDfy();
    }

    // Steps and Assertions
    private void GivenTheInterfaceType()
    {
        _sutType = typeof(IHostedService);
    }

    private void WhenReadingItsMethods()
    {
        _startAsync = _sutType.GetMethod("StartAsync")!;
        _stopAsync = _sutType.GetMethod("StopAsync")!;
    }

    private void ThenMethodsHaveExpectedSignatures()
    {
        _startAsync.Should().NotBeNull();
        _stopAsync.Should().NotBeNull();

        _startAsync.ReturnType.Should().Be(typeof(Task));
        _stopAsync.ReturnType.Should().Be(typeof(Task));

        var startParams = _startAsync.GetParameters();
        var stopParams = _stopAsync.GetParameters();

        startParams.Length.ShouldBe(1);
        stopParams.Length.ShouldBe(1);

        startParams[0].ParameterType.ShouldBe(typeof(CancellationToken));
        stopParams[0].ParameterType.ShouldBe(typeof(CancellationToken));

        // Optional/default parameter is declared in the interface; some runtimes don't expose default value for struct via reflection,
        // so we assert that the parameter is optional OR has a default value or both.
        (startParams[0].IsOptional || startParams[0].HasDefaultValue).ShouldBeTrue();
        (stopParams[0].IsOptional || stopParams[0].HasDefaultValue).ShouldBeTrue();

        // JetBrains annotations may be compiled out via Conditional attribute; do not assert presence at runtime.
    }
}
