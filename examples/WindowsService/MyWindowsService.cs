using HostBridge.Abstractions;
using HostBridge.Core;
using HostBridge.WindowsService;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace WindowsService;

public class MyWindowsService : HostBridgeServiceBase
{
    protected override ILegacyHost BuildHost()
    {
        var host = new LegacyHostBuilder()
            .ConfigureLogging(lb => lb.AddConsole())
            .ConfigureServices((ctx, services) =>
            {
                services.AddOptions();
                services.AddHostedService<HeartbeatService>();
                services.AddSingleton<IWorker, Worker>();
            })
            .Build();
        
        HB.Initialize(host);

        return host;
    }
}