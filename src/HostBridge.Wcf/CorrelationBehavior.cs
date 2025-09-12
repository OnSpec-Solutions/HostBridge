using System.Linq;

namespace HostBridge.Wcf;

/// <summary>
/// WCF contract behavior that installs <see cref="CorrelationDispatchInspector"/> on the dispatch runtime
/// so that a correlation identifier can be read from incoming requests and applied to logging scope.
/// </summary>
/// <remarks>
/// The behavior is idempotent for a given <see cref="DispatchRuntime"/> and will not add duplicate inspectors.
/// It is attached automatically by <see cref="DiServiceHost"/> based on <see cref="CorrelationOptions"/>.
/// </remarks>
/// <param name="headerName">The SOAP/HTTP header name to probe for a correlation id (e.g., "X-Correlation-Id").</param>
internal sealed class CorrelationBehavior(string headerName) : IContractBehavior
{
    /// <summary>
    /// Adds a single <see cref="CorrelationDispatchInspector"/> to the dispatch runtime if one is not already present.
    /// </summary>
    /// <param name="c">The contract description.</param>
    /// <param name="e">The service endpoint.</param>
    /// <param name="d">The dispatch runtime being configured.</param>
    public void ApplyDispatchBehavior(ContractDescription c, ServiceEndpoint e, DispatchRuntime d)
    {
        // idempotent: don't add twice
        if (!d.MessageInspectors.OfType<CorrelationDispatchInspector>().Any())
        {
            d.MessageInspectors.Add(new CorrelationDispatchInspector(headerName));
        }
    }

    /// <summary>No binding parameters are required.</summary>
    public void AddBindingParameters(ContractDescription c, ServiceEndpoint e, BindingParameterCollection p) { }

    /// <summary>No client behavior is applied.</summary>
    public void ApplyClientBehavior(ContractDescription c, ServiceEndpoint e, ClientRuntime r) { }

    /// <summary>No validation is necessary.</summary>
    public void Validate(ContractDescription c, ServiceEndpoint e) { }
}