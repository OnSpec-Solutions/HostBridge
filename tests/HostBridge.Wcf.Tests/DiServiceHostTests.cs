namespace HostBridge.Wcf.Tests;

public class DiServiceHostTests
{
    [ServiceContract]
    public interface ICalc
    {
        [OperationContract]
        int Add(int a, int b);
    }

    public sealed class CalcService : ICalc
    {
        public int Add(int a, int b) => a + b;
    }

    [Fact]
    public void OnOpening_adds_DiInstanceProvider_behavior_to_each_contract()
    {
        using var host = (ServiceHost)Activator.CreateInstance(
            typeof(DiServiceHost),
            BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public,
            binder: null,
            args: [typeof(CalcService), Array.Empty<Uri>()],
            culture: null)!;

        // Add a dummy endpoint so we have ContractDescription instances accessible publicly
        host.AddServiceEndpoint(typeof(ICalc), new BasicHttpBinding(), "http://localhost:1/");

        // Call protected OnOpening via reflection to avoid actually opening endpoints/ports
        var onOpening = typeof(ServiceHostBase).GetMethod("OnOpening", BindingFlags.Instance | BindingFlags.NonPublic);
        onOpening.ShouldNotBeNull();
        onOpening!.Invoke(host, null);

        // Assert that for every endpoint contract we added a DiInstanceProvider behavior
        host.Description.Should().NotBeNull();
        host.Description.Endpoints.Should().NotBeEmpty();
        foreach (var ep in host.Description.Endpoints)
        {
            var cd = ep.Contract;
            cd.Behaviors.Any(b => b.GetType().Name == nameof(DiInstanceProvider)).Should().BeTrue();
        }
    }

    [Fact]
    public void Factory_creates_DiServiceHost_instance()
    {
        var factory = new DiServiceHostFactory();
        var mi = typeof(DiServiceHostFactory).GetMethod("CreateServiceHost",
            BindingFlags.Instance | BindingFlags.NonPublic, binder: null, types: [typeof(Type), typeof(Uri[])],
            modifiers: null);
        mi.ShouldNotBeNull();
        var svcHost = (ServiceHost)mi!.Invoke(factory, [typeof(CalcService), Array.Empty<Uri>()])!;
        svcHost.Should().BeOfType<DiServiceHost>();
    }
}