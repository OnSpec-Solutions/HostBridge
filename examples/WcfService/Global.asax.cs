using System;
using System.Web;
using HostBridge.Core;
using HostBridge.Diagnostics;
using HostBridge.Examples.Common;
using HostBridge.Wcf;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace WcfService
{
    public class Global : HttpApplication
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
            
            HostBridgeWcf.Initialize(host);
        }
        
#if DEBUG        
        private static volatile bool s_aspNetVerified;
#endif
        
        protected void Application_BeginRequest()
        {
#if DEBUG
            if (s_aspNetVerified) return;
            new HostBridgeVerifier()
                .Add(WcfChecks.VerifyWcf)
                .ThrowIfCritical();
            s_aspNetVerified = true;
#endif
        }
    }
}