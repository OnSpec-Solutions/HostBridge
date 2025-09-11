using System;
using FluentAssertions;

using Microsoft.Extensions.Configuration;
using Shouldly;
using TestStack.BDDfy;
using Xunit;

namespace HostBridge.Abstractions.Tests;

public class HostContextTests
{
    private sealed class TestEnvironment(string name) : IHostContext
    {
        public string EnvironmentName { get; } = name;
    }

    // Fields
    private IConfiguration _configuration = null!;
    private IHostContext _environment = null!;
    private HostContext _sut = null!;

    // Scenarios (grouped by target type)
    // HostContext
    [Fact]
    public void Given_a_configuration_and_environment_When_constructing_HostContext_Then_it_exposes_those_instances()
    {
        this.Given(_ => GivenAConfiguration())
            .And(_ => GivenAnEnvironment("Dev"))
            .When(_ => WhenConstructingTheHostContext())
            .Then(_ => ThenTheConfigurationAndEnvironmentAreExposed())
            .BDDfy();
    }

    [Fact]
    public void Given_HostContext_type_When_inspecting_its_contract_Then_it_exposes_Config_and_Environment_properties()
    {
        this.Given(_ => GivenTheHostContextType())
            .When(_ => WhenReadingItsPublicProperties())
            .Then(_ => ThenConfigurationAndEnvironmentAreReadable())
            .BDDfy();
    }

    [Fact]
    public void Given_HostContext_type_When_inspecting_class_Then_it_is_sealed_and_has_expected_property_types()
    {
        this.Given(_ => GivenTheHostContextType())
            .When(_ => WhenReadingItsPublicProperties())
            .Then(_ => ThenHostContextIsSealedAndPropertyTypesAreExpected())
            .BDDfy();
    }

    [Fact]
    public void Given_null_dependencies_When_constructing_HostContext_Then_properties_can_be_null_and_accessed_without_throwing()
    {
        // Even though the API is annotated as non-nullable, it does not guard at runtime. This ensures edge behavior is stable.
        IConfiguration? cfg = null;
        IHostContext? env = null;

        var sut = new HostContext(cfg!, env!);

        sut.Configuration.ShouldBeNull();
        sut.Environment.ShouldBeNull();
    }

    // IHostContext
    [Fact]
    public void Given_IHostContext_interface_When_inspecting_its_contract_Then_it_exposes_EnvironmentName_property()
    {
        this.Given(_ => GivenTheIHostContextType())
            .When(_ => WhenGettingTheEnvironmentNameProperty())
            .Then(_ => ThenThePropertyIsAReadableString())
            .BDDfy();
    }

    // Steps and Assertions
    private void GivenAConfiguration()
    {
        _configuration = new ConfigurationBuilder()
            .AddInMemoryCollection()
            .Build();
    }

    private void GivenAnEnvironment(string name)
    {
        _environment = new TestEnvironment(name);
    }

    private void WhenConstructingTheHostContext()
    {
        _sut = new HostContext(_configuration, _environment);
    }

    private void ThenTheConfigurationAndEnvironmentAreExposed()
    {
        _sut.Configuration.ShouldBeSameAs(_configuration);
        _sut.Environment.ShouldBeSameAs(_environment);
    }

    private Type _iHostContextType = null!;
    private System.Reflection.PropertyInfo? _environmentNameProp;

    private void GivenTheIHostContextType()
    {
        _iHostContextType = typeof(IHostContext);
    }

    private void WhenGettingTheEnvironmentNameProperty()
    {
        _environmentNameProp = _iHostContextType.GetProperty(nameof(IHostContext.EnvironmentName));
    }

    private void ThenThePropertyIsAReadableString()
    {
        _environmentNameProp.Should().NotBeNull();
        _environmentNameProp!.PropertyType.Should().Be(typeof(string));
        _environmentNameProp.CanRead.Should().BeTrue();
    }

    private Type _hostContextType = null!;
    private System.Reflection.PropertyInfo? _configurationProp;
    private System.Reflection.PropertyInfo? _environmentProp;

    private void GivenTheHostContextType()
    {
        _hostContextType = typeof(HostContext);
    }

    private void WhenReadingItsPublicProperties()
    {
        _configurationProp = _hostContextType.GetProperty(nameof(HostContext.Configuration));
        _environmentProp = _hostContextType.GetProperty(nameof(HostContext.Environment));
    }

    private void ThenConfigurationAndEnvironmentAreReadable()
    {
        _configurationProp.Should().NotBeNull();
        _configurationProp!.CanRead.Should().BeTrue();

        _environmentProp.Should().NotBeNull();
        _environmentProp!.CanRead.Should().BeTrue();
    }

    private void ThenHostContextIsSealedAndPropertyTypesAreExpected()
    {
        _hostContextType.IsSealed.Should().BeTrue();
        _configurationProp!.PropertyType.Should().Be(typeof(IConfiguration));
        _environmentProp!.PropertyType.Should().Be(typeof(IHostContext));
    }
}
