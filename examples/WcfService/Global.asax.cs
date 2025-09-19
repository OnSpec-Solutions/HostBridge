using System;
using System.Web;
using HostBridge.Core;
using HostBridge.Diagnostics;
using HostBridge.Examples.Common;
using HostBridge.Wcf;
using HostBridge.Abstractions;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace WcfService
{
    public class Global : HttpApplication
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
                })
                .Build();
            
            s_host = host;
            HostBridgeWcf.Initialize(host);
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
                .Add(WcfChecks.VerifyWcf)
                .ThrowIfCritical();
            s_aspNetVerified = true;
#else
            // Log diagnostics in production
            var logger = HB.Root?.GetRequiredService<ILoggerFactory>().CreateLogger("Diagnostics");
            if (logger != null)
            {
                new HostBridgeVerifier()
                    .Add(WcfChecks.VerifyWcf)
                    .Log(logger);
            }
#endif
        }
    }
}