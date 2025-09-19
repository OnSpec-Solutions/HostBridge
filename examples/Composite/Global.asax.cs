using System;
using System.Web;
using System.Web.Http;
using System.Web.Http.Dispatcher;
using HostBridge.AspNet;
using HostBridge.Core;
using HostBridge.Diagnostics;
using HostBridge.Examples.Common;
using HostBridge.Wcf;
using HostBridge.WebApi2;
using HostBridge.Abstractions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Composite
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

            AspNetBootstrapper.Initialize(host);
            HB.Initialize(host);
            HostBridgeWcf.Initialize(host);

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
        private static volatile bool s_verified;
#endif
        protected void Application_BeginRequest()
        {
#if DEBUG
            if (s_verified) return;
            new HostBridgeVerifier()
                .Add(AspNetChecks.VerifyAspNet)
                .Add(WcfChecks.VerifyWcf)
                .ThrowIfCritical();
            s_verified = true;
#else
            // Log diagnostics once per app lifetime in production
            var logger = AspNetBootstrapper.RootServices?.GetRequiredService<ILoggerFactory>().CreateLogger("Diagnostics");
            if (logger != null)
            {
                new HostBridgeVerifier()
                    .Add(AspNetChecks.VerifyAspNet)
                    .Add(WcfChecks.VerifyWcf)
                    .Log(logger);
            }
#endif
        }
    }
}