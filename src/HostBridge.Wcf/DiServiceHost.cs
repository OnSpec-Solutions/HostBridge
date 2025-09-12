using System.Linq;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

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
    // HostBridge.Wcf/DiServiceHost.cs
    protected override void OnOpening()
    {
        base.OnOpening();

        // Root may be null in unit tests/design-time. Do not throw; simply skip correlation wiring.
        var root = HostBridgeWcf.RootServices;
        var opts = root?.GetService<IOptions<CorrelationOptions>>()?.Value ?? new CorrelationOptions();

        foreach (var cd in ImplementedContracts.Values)
        {
            // Always add DI per-contract behavior
            if (!cd.Behaviors.OfType<DiInstanceProvider>().Any())
                cd.Behaviors.Add(new DiInstanceProvider(cd.ContractType));

            // Conditionally add correlation behavior
            if (opts.Enabled && ShouldAttach(opts, cd))
            {
                if (!cd.Behaviors.OfType<CorrelationBehavior>().Any())
                    cd.Behaviors.Add(new CorrelationBehavior(opts.HeaderName));
            }
        }

        return;

        bool ShouldAttach(CorrelationOptions o, ContractDescription cd)
        {
            // Contract allow/deny lists
            if (o.ExcludeContracts?.Contains(cd.ContractType.FullName) == true)
            {
                return false;
            }
            if (o.IncludeContracts is { Length: > 0 } && !o.IncludeContracts.Contains(cd.ContractType.FullName))
            {
                return false;
            }
            if (cd.ContractType.IsDefined(typeof(DisableCorrelationAttribute), inherit: true))
            {
                return false;
            }
            if (cd.ConfigurationName is { } name &&
                cd.ContractType.Assembly.GetType(name)?.IsDefined(typeof(DisableCorrelationAttribute), true) == true)
            {
                return false;
            }
            if (o.IncludeBindings is not { Length: > 0 })
            {
                return true;
            }

            // Binding filter (checks endpoints that use this contract)
            var shouldAttach = Description.Endpoints
                .Where(ep => ep.Contract == cd)
                .Any(ep => o.IncludeBindings.Contains(ep.Binding?.GetType().Name, StringComparer.OrdinalIgnoreCase)
                           || o.IncludeBindings.Contains(ep.Binding?.Name ?? "", StringComparer.OrdinalIgnoreCase));

            if (!shouldAttach)
            {
                return false;
            }

            // Log only if root available
            if (root is not null)
            {
                var log = root.GetRequiredService<ILoggerFactory>().CreateLogger("HostBridge.Wcf");
                log.LogInformation("Correlation enabled for contract {Contract} (header: {Header})", cd.ContractType.FullName, opts.HeaderName);
            }

            return true;
        }
    }
}