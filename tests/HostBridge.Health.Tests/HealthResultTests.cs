using FluentAssertions;

using Shouldly;
using TestStack.BDDfy;
using Xunit;

namespace HostBridge.Health.Tests;

public class HealthResultTests
{
    private string? _desc;
    private HealthResult _result = null!;

    [Fact]
    public void Given_description_When_Healthy_factory_is_called_Then_it_returns_expected_result()
    {
        this.Given(_ => GivenDescription("All good"))
            .When(_ => WhenHealthyFactoryCalled())
            .Then(_ => ThenStatusIs(HealthStatus.Healthy))
            .And(_ => ThenDescriptionIs("All good"))
            .BDDfy();
    }

    [Fact]
    public void Given_null_description_When_Healthy_factory_is_called_Then_it_returns_expected_result()
    {
        this.Given(_ => GivenDescription(null))
            .When(_ => WhenHealthyFactoryCalled())
            .Then(_ => ThenStatusIs(HealthStatus.Healthy))
            .And(_ => ThenDescriptionIs(null))
            .BDDfy();
    }

    [Fact]
    public void Given_description_When_Degraded_factory_is_called_Then_it_returns_expected_result()
    {
        this.Given(_ => GivenDescription("Some dependencies are slow"))
            .When(_ => WhenDegradedFactoryCalled())
            .Then(_ => ThenStatusIs(HealthStatus.Degraded))
            .And(_ => ThenDescriptionIs("Some dependencies are slow"))
            .BDDfy();
    }

    [Fact]
    public void Given_description_When_Unhealthy_factory_is_called_Then_it_returns_expected_result()
    {
        this.Given(_ => GivenDescription("Down"))
            .When(_ => WhenUnhealthyFactoryCalled())
            .Then(_ => ThenStatusIs(HealthStatus.Unhealthy))
            .And(_ => ThenDescriptionIs("Down"))
            .BDDfy();
    }

    [Fact]
    public void Given_constructor_When_properties_read_Then_values_match()
    {
        var r1 = new HealthResult(HealthStatus.Healthy);
        r1.Status.Should().Be(HealthStatus.Healthy);
        r1.Description.Should().BeNull();

        var r2 = new HealthResult(HealthStatus.Unhealthy, "bad");
        r2.Status.Should().Be(HealthStatus.Unhealthy);
        r2.Description.Should().Be("bad");
    }

    private void GivenDescription(string? desc) => _desc = desc;

    private void WhenHealthyFactoryCalled() => _result = HealthResult.Healthy(_desc);
    private void WhenDegradedFactoryCalled() => _result = HealthResult.Degraded(_desc);
    private void WhenUnhealthyFactoryCalled() => _result = HealthResult.Unhealthy(_desc);

    private void ThenStatusIs(HealthStatus status) => _result.Status.ShouldBe(status);
    private void ThenDescriptionIs(string? expected)
    {
        if (expected is null)
            _result.Description.ShouldBeNull();
        else
            _result.Description.ShouldBe(expected);
    }
}
