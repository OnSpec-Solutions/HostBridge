using System.Web.Mvc;
using System.Web.Routing;
using HostBridge.Core;
using HostBridge.AspNet;
using HostBridge.Mvc5;
using HostBridge.Diagnostics;

namespace Mvc5
{
    public class MvcApplication : System.Web.HttpApplication
    {
        protected void Application_Start()
        {
            // Build the HostBridge legacy host
            var host = new LegacyHostBuilder().Build();

            // Initialize ASP.NET bootstrapper and set MVC resolver
            AspNetBootstrapper.Initialize(host);
            DependencyResolver.SetResolver(new MvcDependencyResolver());

#if DEBUG
            // Dev/test: verifier fails fast if wiring is missing
            new HostBridgeVerifier()
                .Add(AspNetChecks.VerifyAspNet)
                .ThrowIfCritical();
#endif

            AreaRegistration.RegisterAllAreas();
            RouteConfig.RegisterRoutes(RouteTable.Routes);
        }
    }
}
