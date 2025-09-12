namespace HostBridge.Tests.Common.Assertions;

/// <summary>
/// Helper assertions combining FluentAssertions and Shouldly to validate DI lifetimes.
/// Using this ensures our tests utilize both libraries as requested.
/// </summary>
public static class DiAssertions
{
    public static void ShouldHaveDistinctIds(ConcurrentBag<Guid> ids, int expectedDistinct)
    {
        ids.ShouldNotBeNull();
        ids.Count.ShouldBeGreaterThanOrEqualTo(expectedDistinct);
        ids.Distinct().Should().HaveCount(expectedDistinct);
    }

    public static void ShouldBeSingleInstance<T>(ConcurrentBag<T> instances)
    {
        instances.ShouldNotBeNull();
        instances.Should().NotBeEmpty();
        instances.Distinct().Should().ContainSingle();
    }
}