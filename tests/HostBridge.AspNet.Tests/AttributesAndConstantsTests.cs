namespace HostBridge.AspNet.Tests;

public class AttributesAndConstantsTests
{
    private string _scopeKey = null!;
    private FromServicesAttribute _attr = null!;

    [Fact]
    public void Constants_ScopeKey_has_expected_value()
    {
        this.Given(_ => GivenTheScopeKey())
            .Then(_ => ThenScopeKeyIsExpected())
            .BDDfy();
    }

    [Fact]
    public void FromServicesAttribute_can_be_instantiated()
    {
        this.Given(_ => GivenAnAttributeInstance())
            .Then(_ => ThenAttributeIsInstantiatedAndAssignable())
            .BDDfy();
    }

    private void GivenTheScopeKey()
    {
        _scopeKey = Constants.ScopeKey;
    }

    private void ThenScopeKeyIsExpected()
    {
        _scopeKey.Should().Be("HostBridge.Scope");
    }

    private void GivenAnAttributeInstance()
    {
        _attr = new FromServicesAttribute();
    }

    private void ThenAttributeIsInstantiatedAndAssignable()
    {
        _attr.Should().NotBeNull();
        _attr.Should().BeAssignableTo<Attribute>();
    }
}