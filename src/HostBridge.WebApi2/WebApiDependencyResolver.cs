using HostBridge.AspNet;

namespace HostBridge.WebApi2;

/*
config.DependencyResolver = new HostBridge.WebApi2.WebApiDependencyResolver();
 */

/// <summary>
/// ASP.NET Web API 2 dependency resolver that bridges to Microsoft.Extensions.DependencyInjection.
/// </summary>
/// <remarks>
/// Configure with <c>config.DependencyResolver = new WebApiDependencyResolver();</c>
/// Resolution delegates to <see cref="AspNetRequest.RequestServices"/>, which is a per-request scope when
/// <see cref="HostBridge.AspNet.AspNetRequestScopeModule"/> is installed. BeginScope returns a lightweight adapter
/// because the HttpModule owns the actual request scope lifecycle.
/// </remarks>
public sealed class WebApiDependencyResolver : IDependencyResolver
{
    /// <inheritdoc />
    public IDependencyScope BeginScope() => new ScopeAdapter();

    /// <inheritdoc />
    public object GetService(Type serviceType) =>
        AspNetRequest.RequestServices.GetService(serviceType);

    /// <inheritdoc />
    public IEnumerable<object?> GetServices(Type serviceType) =>
        AspNetRequest.RequestServices.GetServices(serviceType);

    /// <inheritdoc />
    public void Dispose() { /* no-op; Module owns request scope */ }

    private sealed class ScopeAdapter : IDependencyScope
    {
        public object GetService(Type serviceType) =>
            AspNetRequest.RequestServices.GetService(serviceType);

        public IEnumerable<object?> GetServices(Type serviceType) =>
            AspNetRequest.RequestServices.GetServices(serviceType);

        public void Dispose() { /* no-op */ }
    }
}