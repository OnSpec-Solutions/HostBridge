using HostBridge.Core;

/*
// In your global.asax.cs or Program.cs/Startup.cs
services.Configure<CorrelationOptions>(cfg.GetSection("HostBridge:Correlation"));

<!-- In your web.config or app.config -->
<appSettings>
  <add key="HostBridge:Correlation:Enabled" value="true" />
  <add key="HostBridge:Correlation:HeaderName" value="X-Correlation-Id" />
  <add key="HostBridge:Correlation:IncludeContracts:0" value="MyNs.IMyService" />
  <!-- or -->
  <!-- <add key="HostBridge:Correlation:IncludeBindings:0" value="basicHttpBinding" /> -->
</appSettings>
 */

namespace HostBridge.Wcf;

/// <summary>
/// Options controlling WCF correlation id extraction and behavior attachment.
/// </summary>
/// <remarks>
/// Bind from configuration using <c>services.Configure&lt;CorrelationOptions&gt;(cfg.GetSection("HostBridge:Correlation"))</c>.
/// When enabled, <see cref="DiServiceHost"/> will attach <see cref="CorrelationBehavior"/> to implemented contracts
/// that match optional include/exclude filters.
/// </remarks>
public sealed class CorrelationOptions
{
    /// <summary>
    /// Enables adding correlation behavior to WCF contracts hosted by <see cref="DiServiceHost"/>.
    /// </summary>
    public bool Enabled { get; set; } = false;

    /// <summary>
    /// The header name to read the correlation id from. Checked as a SOAP header first, then as an HTTP header.
    /// Defaults to "X-Correlation-Id".
    /// </summary>
    public string HeaderName { get; set; } = "X-Correlation-Id";

    /// <summary>
    /// Optional allow-list of fully-qualified contract type names. If specified, correlation is added only to
    /// these contracts.
    /// </summary>
    public string[]? IncludeContracts { get; set; }

    /// <summary>
    /// Optional deny-list of fully-qualified contract type names. Takes precedence over <see cref="IncludeContracts"/>.
    /// </summary>
    public string[]? ExcludeContracts { get; set; }

    /// <summary>
    /// Optional list of binding names or binding type names to which correlation should be applied (e.g.,
    /// "basicHttpBinding", "wsHttpBinding"). If omitted or empty, correlation applies to all bindings unless
    /// excluded by other filters.
    /// </summary>
    public string[]? IncludeBindings { get; set; }
}