namespace HostBridge.Options.Config.Tests;

public class AppConfigConfigurationTests
{
    private IConfigurationBuilder _builder = null!;
    private IConfigurationRoot _config = null!;

    [Fact]
    public void Given_ConfigurationBuilder_When_AddHostBridgeAppConfig_Then_values_from_AppConfig_are_available()
    {
        this.Given(_ => GivenConfigurationBuilder())
            .When(_ => WhenAddingAppConfig())
            .Then(_ => ThenAppSettingsAreMapped())
            .And(_ => ThenConnectionStringsAreMapped())
            .BDDfy();
    }

    [Fact]
    public void Given_ConfigurationBuilder_When_AddHostBridgeAppConfig_Then_provider_is_present()
    {
        this.Given(_ => GivenConfigurationBuilder())
            .When(_ => WhenAddingAppConfig())
            .Then(_ => ThenTheProviderIsPresent())
            .BDDfy();
    }

    private void GivenConfigurationBuilder() => _builder = new ConfigurationBuilder();

    private void WhenAddingAppConfig() => _config = _builder.AddHostBridgeAppConfig().Build();

    private void ThenAppSettingsAreMapped()
    {
        _config["A"].ShouldBe("1");
        _config["EmptyValue"].ShouldBeEmpty();
        _config["Section:Sub"].ShouldBe("val");
    }

    private void ThenConnectionStringsAreMapped()
    {
        _config["connectionStrings:MainDb"].ShouldBe("Data Source=.;Initial Catalog=Test;Integrated Security=True");
        _config["connectionStrings:Secondary"].ShouldBe("Server=(local);Database=Db2;Trusted_Connection=True;");
    }


    private void ThenTheProviderIsPresent()
    {
        // IConfigurationRoot exposes Providers publicly; verify our provider is present
        _config.Providers.ShouldContain(p => p.GetType().Name == "AppConfigConfigurationProvider");
    }
}