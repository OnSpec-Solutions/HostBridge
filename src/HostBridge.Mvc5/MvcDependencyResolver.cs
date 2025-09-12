using HostBridge.AspNet;

namespace HostBridge.Mvc5;

/*
DependencyResolver.SetResolver(new HostBridge.Mvc5.MvcDependencyResolver());
 */
/// <summary>
/// ASP.NET MVC 5 dependency resolver that bridges to Microsoft.Extensions.DependencyInjection.
/// </summary>
/// <remarks>
/// Register with <c>DependencyResolver.SetResolver(new MvcDependencyResolver());</c>.
/// Resolution delegates to <see cref="AspNetRequest.RequestServices"/>, which is a per-request scope when
/// <see cref="HostBridge.AspNet.AspNetRequestScopeModule"/> is installed.
/// </remarks>
public sealed class MvcDependencyResolver : IDependencyResolver
{
    /// <inheritdoc />
    public object GetService(Type serviceType) =>
        AspNetRequest.RequestServices.GetService(serviceType);

    /// <inheritdoc />
    public IEnumerable<object?> GetServices(Type serviceType) =>
        AspNetRequest.RequestServices.GetServices(serviceType);
}