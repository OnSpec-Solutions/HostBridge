using JetBrains.Annotations;

namespace HostBridge.AspNet;

/*
<system.webServer>
  <modules>
    <add name="HostBridgeRequestScope" type="HostBridge.AspNet.AspNetRequestScopeModule" />
  </modules>
</system.webServer>

 */

/// <summary>
/// ASP.NET IHttpModule that creates a per-request <see cref="IServiceScope"/> and disposes it at the end of the request.
/// </summary>
/// <remarks>
/// Register in web.config under system.webServer/modules. This enables proper scoped lifetimes per request and ensures
/// concurrent requests receive isolated scoped instances with no cross-request bleed.
/// </remarks>
[UsedImplicitly]
public sealed class AspNetRequestScopeModule : IHttpModule
{
    /// <summary>
    /// Subscribes to key ASP.NET events to create and dispose a request DI scope and perform optional property injection.
    /// </summary>
    /// <param name="app">The current ASP.NET <see cref="HttpApplication"/> instance.</param>
    /// <remarks>
    /// Handlers:
    /// - BeginRequest: creates a new <see cref="IServiceScope"/> and stores it on <see cref="HttpContext.Items"/>.
    /// - PreRequestHandlerExecute: if the handler is a WebForms Page, performs property injection for properties
    ///   annotated with <see cref="FromServicesAttribute"/> using the scoped provider.
    /// - EndRequest: disposes the created scope and removes it from <see cref="HttpContext.Items"/>.
    /// </remarks>
    public void Init(HttpApplication app)
    {
        app.BeginRequest += (_, _) => OnBegin();
        app.PreRequestHandlerExecute += (_, _) => OnPreRequestHandlerExecute();
        app.EndRequest += (_, _) => OnEnd();
    }

    /// <summary>
    /// Begins a new per-request DI scope and stores it on the current <see cref="HttpContext"/>.
    /// </summary>
    /// <exception cref="InvalidOperationException">
    /// Thrown if <see cref="AspNetBootstrapper.Initialize(HostBridge.Abstractions.ILegacyHost)"/> has not been called.
    /// </exception>
    internal static void OnBegin()
    {
        var root = AspNetBootstrapper.RootServices ?? throw new InvalidOperationException("HostBridge not initialized.");
        HttpContext.Current.Items[Constants.ScopeKey] = root.CreateScope();
    }

    /// <summary>
    /// If the current handler is a WebForms <see cref="System.Web.UI.Page"/>, injects properties marked with
    /// <see cref="FromServicesAttribute"/> using the scoped service provider.
    /// </summary>
    internal static void OnPreRequestHandlerExecute()
    {
        if (HttpContext.Current.Items[Constants.ScopeKey] is not IServiceScope scope) return;
        if (HttpContext.Current.CurrentHandler is System.Web.UI.Page page)
            InjectProperties(page, scope.ServiceProvider);
    }

    /// <summary>
    /// Disposes the per-request scope and clears the stored reference from the current context.
    /// </summary>
    internal static void OnEnd()
    {
        (HttpContext.Current.Items[Constants.ScopeKey] as IServiceScope)?.Dispose();
        HttpContext.Current.Items.Remove(Constants.ScopeKey);
    }

    /// <inheritdoc />
    public void Dispose() { }

    /// <summary>
    /// Performs simple property injection for writable public properties on the target object that are annotated
    /// with <see cref="FromServicesAttribute"/>.
    /// </summary>
    /// <param name="target">The object whose properties should be populated.</param>
    /// <param name="sp">The service provider to resolve dependencies from (typically the request-scoped provider).</param>
    /// <remarks>
    /// Only properties that are writable, public, and decorated with <see cref="FromServicesAttribute"/> are considered.
    /// If a dependency cannot be resolved, the property is left unchanged.
    /// </remarks>
    private static void InjectProperties(object target, IServiceProvider sp)
    {
        var props = target.GetType()
            .GetProperties(BindingFlags.Instance | BindingFlags.Public)
            .Where(p => p.CanWrite && p.GetCustomAttributes(typeof(FromServicesAttribute), true).Any());
        
        foreach (var p in props)
        {
            var dep = sp.GetService(p.PropertyType);
            if (dep != null) p.SetValue(target, dep, null);
        }
    }
}