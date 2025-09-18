using System;

using HostBridge.AspNet;
using HostBridge.Core;
using HostBridge.Diagnostics;
using HostBridge.Examples.Common;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace WebForms
{
    public class Global : System.Web.HttpApplication
    {
        protected void Application_Start(object sender, EventArgs e)
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