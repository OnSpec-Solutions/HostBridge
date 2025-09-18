using System;

using HostBridge.Core;
using HostBridge.Diagnostics;

using Microsoft.Extensions.Logging;

namespace WindowsService
{
    internal static class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        static void Main(string[] args)
        {
            using var service = new MyWindowsService();
            
#if DEBUG
            service.Start(args);
            
            new HostBridgeVerifier()
                .Add(() => WindowsServiceChecks.VerifyWindowsService(runningAsService: !Environment.UserInteractive))
                .Log(HB.Get<ILogger<MyWindowsService>>());
            
            Console.ReadLine();
            service.Stop();
#else
            ServiceBase.Run(service);
#endif
        }
    }
}
