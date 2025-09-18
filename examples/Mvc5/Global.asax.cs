using System;
using System.Web.Mvc;
using System.Web.Routing;
using HostBridge.Core;
using HostBridge.AspNet;
using HostBridge.Mvc5;
using HostBridge.Diagnostics;
using HostBridge.Examples.Common;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

using Mvc5.Controllers;

namespace Mvc5
{
    public class MvcApplication : System.Web.HttpApplication
    {
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
            
            AspNetBootstrapper.Initialize(host);
            DependencyResolver.SetResolver(new MvcDependencyResolver());

            AreaRegistration.RegisterAllAreas();
            RouteConfig.RegisterRoutes(RouteTable.Routes);
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
