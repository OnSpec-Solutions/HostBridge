using System;

namespace HostBridge.Diagnostics;

public sealed class DiagnosticResult(string code, Severity severity, string message, string? fix = null)
{
    public Severity Severity => severity;

    public override string ToString() =>
        $"{Severity} {code}: {message}" + (fix is null ? "" : $"{Environment.NewLine}Fix: {fix}");
}