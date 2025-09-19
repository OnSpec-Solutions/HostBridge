using System;
using System.Web.Mvc;
using System.Web.Routing;
using HostBridge.Core;
using HostBridge.AspNet;
using HostBridge.Mvc5;
using HostBridge.Diagnostics;
using HostBridge.Examples.Common;
using HostBridge.Abstractions;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

using Mvc5.Controllers;

namespace Mvc5
{
    public class MvcApplication : System.Web.HttpApplication
    {
        private static ILegacyHost? s_host;

        protected void Application_Start(object sender, EventArgs e)
        {
            var host = new LegacyHostBuilder()
                .ConfigureLogging(lb => lb.AddConsole())
                .ConfigureServices((ctx, services) =>
                {
                    services.AddOptions();
                    services.AddScoped<IMyScoped, MyScoped>();
                    services.AddTransient<HomeController>();
                })
                .Build();
            
            s_host = host;
            AspNetBootstrapper.Initialize(host);
            DependencyResolver.SetResolver(new MvcDependencyResolver());

            AreaRegistration.RegisterAllAreas();
            RouteConfig.RegisterRoutes(RouteTable.Routes);
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
            var logger = AspNetBootstrapper.RootServices?.GetRequiredService<ILoggerFactory>().CreateLogger("Diagnostics");
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
