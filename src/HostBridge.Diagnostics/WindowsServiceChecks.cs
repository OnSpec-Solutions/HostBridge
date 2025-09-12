using System.Collections.Generic;

namespace HostBridge.Diagnostics;

public static class WindowsServiceChecks
{
    public static IEnumerable<DiagnosticResult> VerifyWindowsService(bool runningAsService)
    {
        if (!runningAsService)
        {
            yield return new DiagnosticResult(
                "HB-SVC-INFO", Severity.Info,
                "Running in console/debug mode; SCM checks skipped.",
                "When installed as a service, OnStop/OnShutdown should call ILegacyHost.StopAsync(ct) and Dispose().");
        }
    }
}