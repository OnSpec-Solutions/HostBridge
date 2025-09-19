using System;
using System.Web.Http;
using System.Web.Http.Dispatcher;
using HostBridge.Core;
using HostBridge.AspNet;
using HostBridge.WebApi2;
using HostBridge.Diagnostics;
using HostBridge.Examples.Common;
using HostBridge.Abstractions;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace WebApi2
{
    public class WebApiApplication : System.Web.HttpApplication
    {
        private static ILegacyHost? s_host;

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

            s_host = host;

            AspNetBootstrapper.Initialize(host);
            HB.Initialize(host);
            
            GlobalConfiguration.Configure(WebApiConfig.Register);
            GlobalConfiguration.Configuration.DependencyResolver = new WebApiDependencyResolver();
            GlobalConfiguration.Configuration.Services.Replace(typeof(IHttpControllerActivator), new HostBridgeControllerActivator());
        }

        protected void Application_End(object sender, EventArgs e)
        {
            if (s_host == null) return;
            try { s_host.StopAsync().GetAwaiter().GetResult(); }
            catch { /* swallow on shutdown */ }
            finally { s_host.Dispose(); s_host = null; }
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
#else
            // Log diagnostics in production
            var logger = AspNetBootstrapper.RootServices?.GetRequiredService<Microsoft.Extensions.Logging.ILoggerFactory>().CreateLogger("Diagnostics");
            if (logger != null)
            {
                new HostBridgeVerifier()
                    .Add(AspNetChecks.VerifyAspNet)
                    .Log(logger);
            }
#endif
        }
    }
}
