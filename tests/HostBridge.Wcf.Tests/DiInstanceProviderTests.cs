using HostBridge.Abstractions;
using HostBridge.Tests.Common.Fakes;

namespace HostBridge.Wcf.Tests;

public class DiInstanceProviderTests
{
    private static ILegacyHost BuildHost(IServiceCollection? services = null)
    {
        services ??= new ServiceCollection();
        services.AddSingleton<SingletonDep>();
        services.AddScoped<ScopedDep>();
        services.AddTransient<WcfService>();
        var sp = services.BuildServiceProvider();
        return new FakeLegacyHost(sp);
    }

    private static object CallOnce(Type serviceType)
    {
        var provider = new DiInstanceProvider(serviceType);
        var ic = new InstanceContext(new object());
        return provider.GetInstance(ic);
    }

    [Fact]
    public void Per_call_scope_creates_distinct_scoped_dependencies()
    {
        // Reset RootServices to ensure isolation from other tests
        var pi = typeof(HostBridgeWcf).GetProperty("RootServices");
        var setter = pi!.GetSetMethod(nonPublic: true);
        setter!.Invoke(null, [null]);

        HostBridgeWcf.Initialize(BuildHost());

        var s1 = (WcfService)CallOnce(typeof(WcfService));
        var s2 = (WcfService)CallOnce(typeof(WcfService));

        s1.Scoped.Id.Should().NotBe(s2.Scoped.Id);
        s1.Singleton.ShouldBeSameAs(s2.Singleton);
    }

    [Fact]
    public void Scope_is_disposed_on_release()
    {
        HostBridgeWcf.Initialize(BuildHost());
        var provider = new DiInstanceProvider(typeof(WcfService));
        var ic = new InstanceContext(new object());
        var instance = (WcfService)provider.GetInstance(ic);
        provider.ReleaseInstance(ic, instance);

        // disposing the instance should not be required; but verify scoped disposable was disposed by checking flag
        instance.Scoped.Disposed.ShouldBeTrue();
    }

    private sealed class TestHost(IServiceProvider sp) : ILegacyHost
    {
        public IServiceProvider ServiceProvider { get; } = sp;

        public System.Threading.Tasks.Task StartAsync(System.Threading.CancellationToken ct = default) =>
            System.Threading.Tasks.Task.CompletedTask;

        public System.Threading.Tasks.Task StopAsync(System.Threading.CancellationToken ct = default) =>
            System.Threading.Tasks.Task.CompletedTask;

        public void Dispose() { }
    }

    public sealed class SingletonDep
    {
    }

    public sealed class ScopedDep : IDisposable
    {
        public Guid Id { get; } = Guid.NewGuid();
        public bool Disposed { get; private set; }
        public void Dispose() => Disposed = true;
    }

    public sealed class WcfService(SingletonDep singleton, ScopedDep scoped)
    {
        public SingletonDep Singleton { get; } = singleton;
        public ScopedDep Scoped { get; } = scoped;
    }
}