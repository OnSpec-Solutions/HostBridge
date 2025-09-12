namespace HostBridge.AspNet;

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
    public static string ScopeKey => "HostBridge.Scope";
}