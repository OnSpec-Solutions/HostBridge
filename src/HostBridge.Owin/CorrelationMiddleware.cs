using System.Collections.Generic;
using HostBridge.Abstractions;
using HostBridge.AspNet;
using HostBridge.Core;
using Microsoft.Extensions.Logging;

namespace HostBridge.Owin;

public static class CorrelationMiddleware
{
    public static void UseHostBridgeCorrelation(this IAppBuilder app)
    {
        if (app == null) throw new ArgumentNullException(nameof(app));

        app.Use(async (env, next) =>
        {
            // Root provider (already set by AspNetBootstrapper)
            var root = AspNetBootstrapper.RootServices
                       ?? throw new InvalidOperationException("HostBridge not initialized (AspNetBootstrapper.Initialize(host)).");

            var logger = root.GetRequiredService<ILoggerFactory>().CreateLogger("Correlation");

            const string headerName = Constants.CorrelationHeaderName;

            // OWIN request headers live on the environment dictionary under "owin.RequestHeaders"
            string? incoming = null;
            IDictionary<string, object>? envDict = env as IDictionary<string, object>;
            object? headersObj = null;
            if (envDict != null && envDict.ContainsKey("owin.RequestHeaders"))
            {
                headersObj = envDict["owin.RequestHeaders"];
            }
            if (headersObj is IDictionary<string, string[]> headers)
            {
                if (headers.ContainsKey(headerName))
                {
                    var values = headers[headerName];
                    if (values != null && values.Length > 0) incoming = values[0];
                }
            }

            using var corr = Correlation.Begin(logger, incoming, headerName);
            await next().ConfigureAwait(false);
            // disposed by using at end of request
        });
    }
}
