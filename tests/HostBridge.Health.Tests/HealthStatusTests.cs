using System;
using FluentAssertions;

using Shouldly;
using Xunit;

namespace HostBridge.Health.Tests;

public class HealthStatusTests
{
    [Fact]
    public void Given_enum_When_values_retrieved_Then_expected_members_and_order()
    {
        // Validate defined names (ordering preserved)
        Enum.GetNames(typeof(HealthStatus)).Should().ContainInOrder("Healthy", "Degraded", "Unhealthy");

        // Validate underlying numeric ordering
        ((int)HealthStatus.Healthy).ShouldBe(0);
        ((int)HealthStatus.Degraded).ShouldBe(1);
        ((int)HealthStatus.Unhealthy).ShouldBe(2);

        // Roundtrip parse
        ((HealthStatus)Enum.Parse(typeof(HealthStatus), "Degraded")).Should().Be(HealthStatus.Degraded);
    }
}
