using System.Web.Http;
using HostBridge.Core;
using HostBridge.AspNet;
using HostBridge.WebApi2;
using HostBridge.Diagnostics;

namespace WebApi2
{
    public class WebApiApplication : System.Web.HttpApplication
    {
        protected void Application_Start()
        {
            var host = new LegacyHostBuilder().Build();

            AspNetBootstrapper.Initialize(host);
            GlobalConfiguration.Configure(WebApiConfig.Register);
            GlobalConfiguration.Configuration.DependencyResolver = new WebApiDependencyResolver();

#if DEBUG
            new HostBridgeVerifier()
                .Add(AspNetChecks.VerifyAspNet)
                .ThrowIfCritical();
#endif
        }
    }
}
