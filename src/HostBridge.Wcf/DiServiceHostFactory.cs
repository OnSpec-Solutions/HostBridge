namespace HostBridge.Wcf;

/*
<%@ ServiceHost Language="C#" Debug="true"
    Service="MyNs.MyService"
    Factory="HostBridge.Wcf.DiServiceHostFactory" %>
 */

/// <summary>
/// A <see cref="ServiceHostFactory"/> that wires up HostBridge’s WCF DI integration.
/// </summary>
/// <remarks>
/// Example web.config markup:
/// <![CDATA[
/// <%@ ServiceHost Language="C#" Debug="true"
///     Service="MyNs.MyService"
///     Factory="HostBridge.Wcf.DiServiceHostFactory" %>
/// ]]>
/// Call <see cref="HostBridgeWcf.Initialize(HostBridge.Abstractions.ILegacyHost)"/> during application start.
/// </remarks>
public sealed class DiServiceHostFactory : ServiceHostFactory
{
    /// <summary>
    /// Creates a <see cref="ServiceHost"/> that installs the DI instance provider.
    /// </summary>
    protected override ServiceHost CreateServiceHost(Type serviceType, Uri[] baseAddresses)
        => new DiServiceHost(serviceType, baseAddresses);
}