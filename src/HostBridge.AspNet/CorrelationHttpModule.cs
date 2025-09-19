using HostBridge.Abstractions;
using HostBridge.Core;

using Microsoft.Extensions.Logging;

namespace HostBridge.AspNet;

/*
<system.webServer>
  <modules>
    <add name="HostBridgeRequestScope" type="HostBridge.AspNet.AspNetRequestScopeModule" />
    <add name="HostBridgeCorrelation"  type="HostBridge.AspNet.CorrelationHttpModule" />
  </modules>
</system.webServer>
 */

public sealed class CorrelationHttpModule : IHttpModule
{
    private const string Cookie = "HostBridge.CorrelationCookie";

    public void Init(HttpApplication app)
    {
        app.BeginRequest += (_, _) => OnBegin();
        app.EndRequest += (_, _) => OnEnd();
    }

    internal static void OnBegin()
    {
        // Root provider (already set by AspNetBootstrapper)
        var root = AspNetBootstrapper.RootServices!;
        var logger = root.GetRequiredService<ILoggerFactory>().CreateLogger("Correlation");

        const string header = Constants.CorrelationHeaderName;
        var incoming = HttpContext.Current.Request.Headers[header];
        var scope = Correlation.Begin(logger, incoming, header);

        HttpContext.Current.Items[Cookie] = scope;
    }

    internal static void OnEnd()
    {
        (HttpContext.Current.Items[Cookie] as IDisposable)?.Dispose();
        HttpContext.Current.Items.Remove(Cookie);
    }

    public void Dispose() { }
}