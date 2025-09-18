using System.Net.Http;
using System.Web.Http.Controllers;
using System.Web.Http.Dispatcher;
using HostBridge.AspNet;

namespace HostBridge.WebApi2;

/// <summary>
/// Web API 2 IHttpControllerActivator that uses Microsoft.Extensions.DependencyInjection to construct controllers.
/// </summary>
/// <remarks>
/// Falls back to the current ASP.NET request scope via <see cref="AspNetRequest.RequestServices"/>.
/// This allows controllers to use constructor injection without being explicitly registered in the container.
/// </remarks>
public sealed class HostBridgeControllerActivator : IHttpControllerActivator
{
    public IHttpController Create(HttpRequestMessage request, HttpControllerDescriptor controllerDescriptor, Type controllerType)
    {
        var services = AspNetRequest.RequestServices;
        var instance = ActivatorUtilities.CreateInstance(services, controllerType);
        return (IHttpController)instance;
    }
}
