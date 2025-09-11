using System;
using System.Threading;
using System.Threading.Tasks;

using HostBridge.Core;
using HostBridge.Options.Config;

using JetBrains.Annotations;

using Microsoft.Extensions.Logging;

namespace Examples.Console;

[UsedImplicitly]
internal class Program
{
    static async Task Main(string[] args)
    {
        var host = new LegacyHostBuilder()
            .ConfigureAppConfiguration(cfg => cfg.AddHostBridgeAppConfig())
            .ConfigureLogging(lb => lb.AddConsole())
            .ConfigureServices((_, services) =>
            {
                services.AddHostedService<HeartbeatService>();
            })
            .Build();

        using var runFor30Seconds = new CancellationTokenSource(TimeSpan.FromSeconds(30));
        await host.RunAsync(runFor30Seconds.Token, shutdownTimeout: TimeSpan.FromSeconds(10));
    }
}