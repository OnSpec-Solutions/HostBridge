namespace HostBridge.Wcf.Tests;

public class DiInstanceProviderEdgeTests
{
    [Fact]
    public void GetInstance_without_Initialize_throws_InvalidOperation()
    {
        // Force reset of RootServices to simulate uninitialized state
        // First try property setter
        var pi = typeof(HostBridgeWcf).GetProperty("RootServices");
        var setter = pi!.GetSetMethod(nonPublic: true);
        setter!.Invoke(null, new object?[] { null });
        // Also reset via backing field in case setter invocation is ignored
        var backing = typeof(HostBridgeWcf).GetField("<RootServices>k__BackingField",
            BindingFlags.Static | BindingFlags.NonPublic);
        backing!.SetValue(null, null);
        HostBridgeWcf.RootServices.Should().BeNull();

        var provider = new DiInstanceProvider(typeof(object));
        var ic = new InstanceContext(new object());
        Action act = () => provider.GetInstance(ic);
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*not initialized*");
    }

    [Fact]
    public void Contract_behavior_noop_methods_do_not_throw_even_with_nulls()
    {
        var provider = new DiInstanceProvider(typeof(object));
        provider.AddBindingParameters(null, null, null);
        provider.ApplyClientBehavior(null, null, null);
        provider.Validate(null, null);
        // If we reached here, no exceptions were thrown
        true.ShouldBeTrue();
    }

    [Fact]
    public void ApplyDispatchBehavior_with_null_runtime_throws_but_executes_assignment_line()
    {
        var provider = new DiInstanceProvider(typeof(object));
        Action act = () => provider.ApplyDispatchBehavior(null, null, null);
        act.Should().Throw<NullReferenceException>();
    }
}