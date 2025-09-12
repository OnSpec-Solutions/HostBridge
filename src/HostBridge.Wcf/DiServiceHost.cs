namespace HostBridge.Wcf;

/// <summary>
/// A <see cref="ServiceHost"/> that installs <see cref="DiInstanceProvider"/> for each implemented contract
/// so WCF service instances are resolved via Microsoft.Extensions.DependencyInjection with a per-operation scope.
/// </summary>
/// <remarks>
/// This host is created by <see cref="DiServiceHostFactory"/>. During <see cref="OnOpening"/>, it iterates over
/// <see cref="ServiceHostBase.ImplementedContracts"/> and adds a <see cref="DiInstanceProvider"/> behavior to each
/// contract, enabling DI-based resolution of service instances.
/// </remarks>
/// <param name="serviceType">The concrete WCF service implementation type to host.</param>
/// <param name="baseAddresses">Optional base addresses for the hosted service.</param>
internal sealed class DiServiceHost(Type serviceType, params Uri[] baseAddresses)
    : ServiceHost(serviceType, baseAddresses)
{
    /// <summary>
    /// Overrides the opening sequence to attach <see cref="DiInstanceProvider"/> to all implemented contracts.
    /// </summary>
    protected override void OnOpening()
    {
        base.OnOpening();
        foreach (var cd in ImplementedContracts.Values)
        {
            cd.Behaviors.Add(new DiInstanceProvider(cd.ContractType));
        }
    }
}