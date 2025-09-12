namespace HostBridge.Abstractions;

/// <summary>
/// Constants used by HostBridge ASP.NET integration.
/// </summary>
public static class Constants
{
    /// <summary>
    /// HttpContext.Items key under which the current request's IServiceScope is stored.
    /// </summary>
    /// <example>
    /// var scope = (IServiceScope)HttpContext.Current.Items[Constants.ScopeKey];
    /// </example>
    public const string ScopeKey = "HostBridge.Scope";
    public const string CorrelationHeaderName = "X-Correlation-Id";
}