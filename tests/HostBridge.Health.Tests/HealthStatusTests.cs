namespace HostBridge.Health.Tests;

public class HealthStatusTests
{
    private string[] _names = null!;
    private int _healthyValue;
    private int _degradedValue;
    private int _unhealthyValue;
    private HealthStatus _parsed;

    [Fact]
    public void Given_enum_When_values_retrieved_Then_expected_members_and_order()
    {
        this.Given(_ => GivenTheEnumNamesAreRetrieved())
            .And(_ => GivenTheUnderlyingNumericValues())
            .When(_ => WhenParsingDegradedFromString())
            .Then(_ => ThenNamesAreInExpectedOrder())
            .And(_ => ThenUnderlyingNumericOrderingIsExpected())
            .And(_ => ThenParsedValueIsDegraded())
            .BDDfy();
    }

    private void GivenTheEnumNamesAreRetrieved()
    {
        _names = Enum.GetNames(typeof(HealthStatus));
    }

    private void GivenTheUnderlyingNumericValues()
    {
        _healthyValue = (int)HealthStatus.Healthy;
        _degradedValue = (int)HealthStatus.Degraded;
        _unhealthyValue = (int)HealthStatus.Unhealthy;
    }

    private void WhenParsingDegradedFromString()
    {
        _parsed = (HealthStatus)Enum.Parse(typeof(HealthStatus), "Degraded");
    }

    private void ThenNamesAreInExpectedOrder()
    {
        _names.Should().ContainInOrder("Healthy", "Degraded", "Unhealthy");
    }

    private void ThenUnderlyingNumericOrderingIsExpected()
    {
        _healthyValue.ShouldBe(0);
        _degradedValue.ShouldBe(1);
        _unhealthyValue.ShouldBe(2);
    }

    private void ThenParsedValueIsDegraded()
    {
        _parsed.Should().Be(HealthStatus.Degraded);
    }
}