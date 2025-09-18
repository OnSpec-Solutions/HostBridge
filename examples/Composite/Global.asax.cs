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
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Composite
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

            AspNetBootstrapper.Initialize(host);
            HB.Initialize(host);
            HostBridgeWcf.Initialize(host);

            GlobalConfiguration.Configure(WebApiConfig.Register);
            GlobalConfiguration.Configuration.DependencyResolver = new WebApiDependencyResolver();
            GlobalConfiguration.Configuration.Services.Replace(typeof(IHttpControllerActivator), new HostBridgeControllerActivator());
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
#endif
        }
    }
}