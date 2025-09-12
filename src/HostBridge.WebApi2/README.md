# HostBridge.WebApi2

ASP.NET Web API 2 dependency resolver that uses Microsoft.Extensions.DependencyInjection and participates in the per-request scope created by HostBridge.AspNet.

Install: `HostBridge.WebApi2` (net472, net48)

## Wire-up

1) Ensure HostBridge.AspNet is initialized and the request-scope IHttpModule is registered (see that package README).

2) In WebApiConfig.Register(HttpConfiguration config):

```csharp
using System.Web.Http;
using HostBridge.AspNet;
using HostBridge.WebApi2;

public static class WebApiConfig
{
    public static void Register(HttpConfiguration config)
    {
        // build/init elsewhere at app start; shown here for brevity
        var services = new ServiceCollection();
        services.AddScoped<IMyScoped, MyScoped>();
        var sp = services.BuildServiceProvider();
        AspNetBootstrapper.Initialize(new LegacyHost(sp));

        // tell Web API to use DI
        config.DependencyResolver = new WebApiDependencyResolver();

        // routes, formatters, etc.
        config.MapHttpAttributeRoutes();
    }
}
```

## How it works
- WebApiDependencyResolver delegates to AspNetRequest.RequestServices. When the IHttpModule is installed, that is a per-request IServiceScope.
- BeginScope returns a lightweight adapter because the HttpModule owns the actual request scope lifecycle.

## Examples
- See ../../examples/WebApi2