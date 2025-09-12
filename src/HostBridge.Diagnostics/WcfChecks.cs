using System.Collections.Generic;

#if NET472_OR_GREATER
using HostBridge.Wcf;

namespace HostBridge.Diagnostics;

public static class WcfChecks
{
    public static IEnumerable<DiagnosticResult> VerifyWcf()
    {
        if (HostBridgeWcf.RootServices is null)
        {
            yield return new DiagnosticResult(
                "HB-WCF-001", Severity.Critical,
                "HostBridgeWcf.Initialize(host) has not been called.",
                "Call HostBridge.Wcf.HostBridgeWcf.Initialize(host) (e.g., in Global.asax Application_Start).");
        }

        // We can’t see config at runtime easily; give a helpful hint to prevent the common miss.
        yield return new DiagnosticResult(
            "HB-WCF-INFO", Severity.Info,
            "Ensure your service uses DiServiceHostFactory so DI is applied.",
            "In .svc: Factory=\"HostBridge.Wcf.DiServiceHostFactory\" or in <service> element: factory=\"HostBridge.Wcf.DiServiceHostFactory\".");
    }
}
#else
namespace HostBridge.Diagnostics;

public static class WcfChecks
{
    public static IEnumerable<DiagnosticResult> VerifyWcf()
    {
        yield break;
    }
}
#endif