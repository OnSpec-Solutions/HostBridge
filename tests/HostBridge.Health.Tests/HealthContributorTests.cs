using HostBridge.Tests.Common.Fakes;

namespace HostBridge.Health.Tests;

public class HealthContributorTests
{
    private IHealthContributor _contributor = null!;
    private HealthResult _result = null!;

    [Fact]
    public void Given_contributor_When_Check_called_Then_returns_configured_result_and_name()
    {
        this.Given(_ => GivenContributor("db", HealthResult.Degraded("latency")))
            .When(_ => WhenCheckInvoked())
            .Then(_ => ThenNameIs("db"))
            .And(_ => ThenResultIs(HealthStatus.Degraded, "latency"))
            .BDDfy();
    }

    private void GivenContributor(string name, HealthResult result)
    {
        _contributor = new StaticHealthContributor(name, result);
    }

    private void WhenCheckInvoked() => _result = _contributor.Check();

    private void ThenNameIs(string expected) => _contributor.Name.ShouldBe(expected);

    private void ThenResultIs(HealthStatus status, string? description)
    {
        _result.Status.Should().Be(status);
        if (description is null)
            _result.Description.ShouldBeNull();
        else
            _result.Description.ShouldBe(description);
    }
}