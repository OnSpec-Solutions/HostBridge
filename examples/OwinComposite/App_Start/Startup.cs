using System.Web.Http;
using System.Threading;

using HostBridge.AspNet;
using HostBridge.Core;
using HostBridge.Examples.Common;
using HostBridge.Owin;
using HostBridge.Wcf;
using HostBridge.Diagnostics;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Owin;

using Owin;

[assembly: OwinStartup(typeof(OwinComposite.Startup))]

namespace OwinComposite;

public class Startup
{
    public void Configuration(IAppBuilder app)
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

        // Ensure graceful shutdown when the OWIN host disposes
        if (app.Properties.TryGetValue("host.OnAppDisposing", out var tokenObj) && tokenObj is CancellationToken token)
        {
            token.Register(() =>
            {
                try { host.StopAsync().GetAwaiter().GetResult(); }
                catch { /* swallow on shutdown */ }
                finally { host.Dispose(); }
            });
        }

        // HostBridge OWIN scope + correlation
        app.UseHostBridgeRequestScope();
        app.UseHostBridgeCorrelation();

        var config = new HttpConfiguration();
        config.DependencyResolver = new WebApiOwinAwareResolver();
        WebApiConfig.Register(config);
        app.UseWebApi(config);

#if DEBUG
        // Fail fast in dev/test
        new HostBridgeVerifier()
            .Add(AspNetChecks.VerifyAspNet)
            .Add(WcfChecks.VerifyWcf)
            .ThrowIfCritical();
#else
        // Log diagnostics in production
        var logger = host.ServiceProvider.GetRequiredService<ILoggerFactory>().CreateLogger("Diagnostics");
        new HostBridgeVerifier()
            .Add(AspNetChecks.VerifyAspNet)
            .Add(WcfChecks.VerifyWcf)
            .Log(logger);
#endif
    }
}