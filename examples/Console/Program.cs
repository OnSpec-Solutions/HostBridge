// Program.cs (examples/Console)

using System;
using System.Threading;
using System.Threading.Tasks;

using Examples.Console;

using HostBridge.Core;
using HostBridge.Options.Config;

using JetBrains.Annotations;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

[UsedImplicitly]
class Program
{
    static async Task Main()
    {
        var host = new LegacyHostBuilder()
            .ConfigureAppConfiguration(cfg => cfg.AddHostBridgeAppConfig())
            .ConfigureLogging(lb => lb.AddConsole())
            .ConfigureServices((_, services) =>
            {
                services.AddOptions();
                services.AddHostedService<HeartbeatService>();
                services.AddSingleton<IWorker, Worker>();
                services.AddScoped<IOperation, Operation>();
            })
            .Build();

        // Initialize the accessor once
        HB.Initialize(host);

        // Example: run a scoped “operation” in console land
        using (HB.BeginScope())
        {
            var worker = HB.Get<IWorker>();
            await worker.RunAsync();
        }

        // Or run until canceled with a shutdown grace period
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(10));
        await host.RunAsync(cts.Token, shutdownTimeout: TimeSpan.FromSeconds(5));
    }
}