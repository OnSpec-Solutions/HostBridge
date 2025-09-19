using System.Collections.Concurrent;
using HostBridge.Abstractions;
using HostBridge.Tests.Common.Fakes;

namespace HostBridge.Core.Tests;

[Collection("HB.Serial")]
public class HBSideLoadTests
{
    private IServiceProvider _root = null!;

    // Test 1: Basic Required/Optional behavior
    [Fact]
    public void Given_initialized_When_Required_and_Optional_used_Then_behave_as_expected()
    {
        this.Given(_ => GivenInitializedWithHelloString())
            .When(_ => Noop())
            .Then(_ => ThenRequiredReturnsHelloAndOptionalIsNullForUnregisteredType())
            .BDDfy();
    }

    // Test 2: Singleton helper returns same instance across calls/threads
    [Fact]
    public void Given_singleton_registration_When_Singleton_helper_used_multiple_times_and_threads_Then_same_instance_is_returned()
    {
        this.Given(_ => GivenSingletonRegistered())
            .When(_ => WhenCallingSingletonHelperMultipleTimesAndThreads())
            .Then(_ => ThenSingletonHelperMatchesRegisteredSingleton())
            .BDDfy();
    }

    // Test 3: Scoped instances differ across scopes but are same within a single scope
    [Fact]
    public void Given_scoped_registration_When_InScope_used_multiple_times_Then_each_scope_gets_distinct_instance_and_within_scope_same_instance()
    {
        this.Given(_ => GivenScopedRegistered())
            .When(_ => Noop())
            .Then(_ => ThenEachScopeIsDistinctAndWithinScopeSame())
            .BDDfy();
    }

    // Test 4: Concurrency - many parallel scopes do not bleed and each gets its own instance
    [Fact]
    public void Given_scoped_registration_When_concurrent_InScope_operations_run_Then_no_scope_bleed_and_all_instances_distinct_per_scope()
    {
        this.Given(_ => GivenScopedRegistered())
            .When(_ => WhenRunningManyParallelScopes())
            .Then(_ => ThenAllScopesWereDistinct())
            .BDDfy();
    }

    // Givens
    private void GivenInitializedWithHelloString() => GivenHBInitialized(s => s.AddSingleton("hello"));

    private void GivenSingletonRegistered() => GivenHBInitialized(s => s.AddSingleton<ISampleSingleton, SampleSingleton>());

    private void GivenScopedRegistered() => GivenHBInitialized(s => s.AddScoped<ISampleScoped, SampleScoped>());

    private static readonly object _resetLock = new();
    private void GivenHBInitialized(Action<IServiceCollection> configure)
    {
        // Ensure global HB static is reset to avoid cross-test interference even in Release builds
        ResetHBUnsafe();

        var services = new ServiceCollection();
        configure(services);
        _root = services.BuildServiceProvider();
        HB.Initialize(new FakeLegacyHost(_root));
    }

    private static void ResetHBUnsafe()
    {
        lock (_resetLock)
        {
            var hbType = typeof(HB);
            var sRootField = hbType.GetField("s_root", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
            sRootField!.SetValue(null, null);

            var ambientField = hbType.GetField("Ambient", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
            var ambient = ambientField!.GetValue(null);
            if (ambient is not null)
            {
                var ambientType = ambient.GetType(); // AsyncLocal<IServiceScope?>
                var valueProp = ambientType.GetProperty("Value");
                valueProp!.SetValue(ambient, null);
            }
        }
    }

    // Whens
    private void Noop() { }

    private ISampleSingleton? _capturedSingleton;
    private ISampleSingleton? _singletonA;
    private ISampleSingleton? _singletonB;
    private ISampleSingleton[]? _singletonParallel;

    private void WhenCallingSingletonHelperMultipleTimesAndThreads()
    {
        _capturedSingleton = _root.GetRequiredService<ISampleSingleton>();
        _singletonA = HBSideLoad.Singleton<ISampleSingleton>();
        _singletonB = HBSideLoad.Singleton<ISampleSingleton>();

        const int N = 64;
        _singletonParallel = new ISampleSingleton[N];
        Parallel.For(0, N, i => { _singletonParallel[i] = HBSideLoad.Singleton<ISampleSingleton>(); });
    }

    private Guid _scopeId1;
    private Guid _scopeId2;

    private void WhenRunningManyParallelScopes()
    {
        const int N = 100;
        var ids = new ConcurrentBag<Guid>();
        Parallel.For(0, N, _ =>
        {
            var id = HBSideLoad.InScope(sp =>
            {
                var a = sp.GetRequiredService<ISampleScoped>();
                var b = HBSideLoad.Required<ISampleScoped>();
                ReferenceEquals(a, b).Should().BeTrue();
                return a.Id;
            });
            ids.Add(id);
        });
        _parallelScopeIds = ids.ToArray();
    }

    // Thens
    private void ThenRequiredReturnsHelloAndOptionalIsNullForUnregisteredType()
    {
        HBSideLoad.Required<string>().Should().Be("hello");
        HBSideLoad.Optional<ISampleSingleton>().Should().BeNull();
    }

    private void ThenSingletonHelperMatchesRegisteredSingleton()
    {
        ReferenceEquals(_singletonA, _capturedSingleton).Should().BeTrue();
        ReferenceEquals(_singletonB, _capturedSingleton).Should().BeTrue();
        _singletonParallel!.Should().OnlyContain(x => ReferenceEquals(x, _capturedSingleton));

        var fromRootAgain = _root.GetRequiredService<ISampleSingleton>();
        ReferenceEquals(fromRootAgain, _capturedSingleton).Should().BeTrue();
    }

    private Guid[]? _parallelScopeIds;

    private void ThenEachScopeIsDistinctAndWithinScopeSame()
    {
        _scopeId1 = HBSideLoad.InScope(sp =>
        {
            var a = sp.GetRequiredService<ISampleScoped>();
            var b = HBSideLoad.Required<ISampleScoped>();
            ReferenceEquals(a, b).Should().BeTrue();
            return a.Id;
        });

        _scopeId2 = HBSideLoad.InScope(sp => sp.GetRequiredService<ISampleScoped>().Id);

        _scopeId1.Should().NotBe(_scopeId2);
    }

    private void ThenAllScopesWereDistinct()
    {
        _parallelScopeIds.Should().NotBeNull();
        _parallelScopeIds!.Length.Should().Be(100);
        _parallelScopeIds!.Distinct().Count().Should().Be(100);
    }

    // Sample contracts
    public interface ISampleSingleton { Guid Id { get; } }
    public sealed class SampleSingleton : ISampleSingleton { public Guid Id { get; } = Guid.NewGuid(); }

    public interface ISampleScoped { Guid Id { get; } }
    public sealed class SampleScoped : ISampleScoped { public Guid Id { get; } = Guid.NewGuid(); }
}
