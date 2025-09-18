using System.Web.Http;

using HostBridge.AspNet;
using HostBridge.Core;
using HostBridge.Examples.Common;
using HostBridge.Owin;
using HostBridge.Wcf;

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

        app.UseHostBridgeRequestScope();

        var config = new HttpConfiguration();
        config.DependencyResolver = new WebApiOwinAwareResolver();
        WebApiConfig.Register(config);
        app.UseWebApi(config);
    }
}