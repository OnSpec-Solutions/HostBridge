namespace HostBridge.AspNet;

/// <summary>
/// Accessor for the current request's <see cref="IServiceProvider"/> in classic ASP.NET.
/// </summary>
/// <remarks>
/// Requires <see cref="AspNetRequestScopeModule"/> to be registered to get a true per-request scope.
/// If the module isn't registered (e.g., in background work), falls back to the root provider.
/// </remarks>
public static class AspNetRequest
{
    /// <summary>
    /// Gets the ambient service provider for the current HTTP request scope, or the root provider if no scope exists.
    /// </summary>
    public static IServiceProvider RequestServices =>
        (HttpContext.Current?.Items[Constants.ScopeKey] as IServiceScope)?.ServiceProvider
        ?? AspNetBootstrapper.RootServices
        ?? throw new InvalidOperationException("HostBridge not initialized.");
}