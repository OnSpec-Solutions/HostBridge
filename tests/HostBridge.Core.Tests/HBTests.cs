using HostBridge.Abstractions;
using HostBridge.Tests.Common.Fakes;

namespace HostBridge.Core.Tests;

public class HBTests
{
    private IServiceProvider _root = null!;
    private Exception? _ex;
    private IServiceProvider? _before;
    private IServiceProvider? _during;
    private IServiceProvider? _scoped;
    private int _requiredValue;
    private string? _optionalValue;

    private static void DoNothing() { }

    [Fact]
    public void Given_not_initialized_When_accessing_Root_Then_throws()
    {
        this.Given(_ => GivenHBReset())
            .When(_ => WhenAccessingRootExpectingFailure())
            .Then(_ => ThenInvalidOperationIsThrown())
            .BDDfy();
    }

    [Fact]
    public void Given_initialized_When_accessing_Root_and_Current_Then_point_to_root()
    {
        this.Given(_ => GivenInitializedWithHelloString())
            .When(_ => DoNothing())
            .Then(_ => ThenRootAndCurrentAreSame())
            .BDDfy();
    }

    [Fact]
    public void Given_initialized_When_initialized_again_with_same_provider_Then_is_idempotent()
    {
        this.Given(_ => GivenInitializedNoServices())
            .When(_ => WhenInitializeAgainWithSameProvider())
            .Then(_ => ThenNoExceptionIsThrown())
            .BDDfy();
    }

    [Fact]
    public void Given_initialized_When_initialized_again_with_different_provider_Then_throws()
    {
        this.Given(_ => GivenInitializedWith(s => s.AddSingleton<string>("a")))
            .When(_ => WhenInitializeAgainWithDifferentProvider())
            .Then(_ => ThenHasAlreadyBeenInitializedIsThrown())
            .BDDfy();
    }

    [Fact]
    public void Given_ambient_When_begin_scope_Then_current_changes_and_restores_on_dispose()
    {
        this.Given(_ => GivenInitializedNoServices())
            .When(_ => WhenBeginningAmbientScope())
            .Then(_ => ThenAmbientScopeIsAppliedAndRestored())
            .BDDfy();
    }

    [Fact]
    public void Given_root_When_create_scope_Then_returns_scoped_provider_different_from_root()
    {
        this.Given(_ => GivenInitializedNoServices())
            .When(_ => WhenCreateScope())
            .Then(_ => ThenScopeProviderDiffersFromRoot())
            .BDDfy();
    }

    [Fact]
    public void Given_services_When_Get_and_TryGet_are_used_Then_behave_as_expected()
    {
        this.Given(_ => GivenInitializedWithNumber())
            .When(_ => WhenResolvingServices())
            .Then(_ => ThenResolutionsAreCorrect())
            .BDDfy();
    }

    private void GivenHBReset()
    {
#if DEBUG
        HB._ResetForTests();
#endif
    }

    private void GivenInitializedWith(Action<IServiceCollection> configure)
    {
        GivenHBReset();
        var services = new ServiceCollection();
        configure(services);
        _root = services.BuildServiceProvider();
        HB.Initialize(new FakeLegacyHost(_root));
    }

    private void GivenInitializedWithHelloString() => GivenInitializedWith(s => s.AddSingleton<string>("hello"));
    private void GivenInitializedNoServices() => GivenInitializedWith(_ => { });
    private void GivenInitializedWithNumber() => GivenInitializedWith(s => s.AddSingleton(new Number(42)));

    private void WhenAccessingRootExpectingFailure()
    {
        _ex = Assert.Throws<InvalidOperationException>(() => _ = HB.Root);
    }

    private void ThenInvalidOperationIsThrown()
    {
        _ex.Should().BeOfType<InvalidOperationException>();
        _ex!.Message.Should().Contain("HB not initialized");
    }

    private static void ThenRootAndCurrentAreSame()
    {
        HB.Current.Should().BeSameAs(HB.Root);
    }

    private void WhenInitializeAgainWithSameProvider()
    {
        var same = new FakeLegacyHost(_root);
        HB.Initialize(same);
    }

    private static void ThenNoExceptionIsThrown()
    {
        // No-op: reaching this point means no exception was thrown.
        HB.Root.Should().NotBeNull();
    }

    private void WhenInitializeAgainWithDifferentProvider()
    {
        var services = new ServiceCollection();
        var sp2 = services.BuildServiceProvider();
        _ex = Assert.Throws<InvalidOperationException>(() => HB.Initialize(new FakeLegacyHost(sp2)));
    }

    private void ThenHasAlreadyBeenInitializedIsThrown()
    {
        _ex!.Message.Should().Contain("already been initialized");
    }

    private void WhenBeginningAmbientScope()
    {
        _before = HB.Current;
        using (HB.BeginScope())
        {
            _during = HB.Current;
        }
    }

    private void ThenAmbientScopeIsAppliedAndRestored()
    {
        _during.Should().NotBeSameAs(_before);
        HB.Current.Should().BeSameAs(_before);
    }

    private void WhenCreateScope()
    {
        using var scope = HB.CreateScope();
        _scoped = scope.ServiceProvider;
    }

    private void ThenScopeProviderDiffersFromRoot()
    {
        _scoped.Should().NotBeSameAs(HB.Root);
    }

    private void WhenResolvingServices()
    {
        var number = HB.Get<Number>();
        _requiredValue = number.Value;
        _optionalValue = HB.TryGet<string>();
    }

    private void ThenResolutionsAreCorrect()
    {
        _requiredValue.Should().Be(42);
        _optionalValue.Should().BeNull();
    }

    private sealed class Number
    {
        public Number(int value) { Value = value; }
        public int Value { get; }
    }

}