namespace HostBridge.Tests.Common.Fakes;

// Hosted service used only to ensure distinct registrations in tests.
[UsedImplicitly]
public sealed class DistinctHostedServiceB(List<string> calls, string name) : TrackingHostedService(calls, name)
{
    public DistinctHostedServiceB() : this([], "b") { }
}