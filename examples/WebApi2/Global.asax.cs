using System.Web.Http;
using System.Web.Http.Dispatcher;
using HostBridge.Core;
using HostBridge.AspNet;
using HostBridge.WebApi2;
using HostBridge.Diagnostics;
using HostBridge.Examples.Common;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace WebApi2
{
    public class WebApiApplication : System.Web.HttpApplication
    {
        protected void Application_Start()
        {
            var host = new LegacyHostBuilder()
                .ConfigureLogging(lb => lb.AddConsole())
                .ConfigureServices((ctx, services) =>
                {
                    services.AddOptions();
                    services.AddScoped<IMyScoped, MyScoped>();
                })
                .Build();

            AspNetBootstrapper.Initialize(host);
            HB.Initialize(host);
            
            GlobalConfiguration.Configure(WebApiConfig.Register);
            GlobalConfiguration.Configuration.DependencyResolver = new WebApiDependencyResolver();
            GlobalConfiguration.Configuration.Services.Replace(typeof(IHttpControllerActivator), new HostBridgeControllerActivator());
        }
        
#if DEBUG        
        private static volatile bool s_aspNetVerified;
#endif
        
        protected void Application_BeginRequest()
        {
#if DEBUG
            if (s_aspNetVerified) return;
            new HostBridgeVerifier()
                .Add(AspNetChecks.VerifyAspNet)
                .ThrowIfCritical();
            s_aspNetVerified = true;
#endif
        }
    }
}
