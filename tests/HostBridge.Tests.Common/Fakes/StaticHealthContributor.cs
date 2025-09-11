using HostBridge.Health;

namespace HostBridge.Tests.Common.Fakes;

// Health contributor that always returns a predefined result.
public sealed class StaticHealthContributor(string name, HealthResult result) : IHealthContributor
{
    public string Name => name;
    public HealthResult Check() => result;
}
