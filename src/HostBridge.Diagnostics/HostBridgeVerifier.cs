using System;
using System.Collections.Generic;
using System.Linq;

using Microsoft.Extensions.Logging;

namespace HostBridge.Diagnostics;

public sealed class HostBridgeVerifier
{
    private readonly List<Func<IEnumerable<DiagnosticResult>>> _checks = [];

    public HostBridgeVerifier Add(Func<IEnumerable<DiagnosticResult>> check)
    {
        _checks.Add(check);
        return this;
    }

    public IReadOnlyList<DiagnosticResult> Run() =>
        _checks.SelectMany(Safe).ToArray();

    public void ThrowIfCritical()
    {
        var results = Run();
        var bad = results.Where(r => r.Severity is Severity.Critical or Severity.Error).ToArray();
        if (bad.Length == 0) return;

        var banner = "HostBridge configuration verification failed:\n\n" +
                     string.Join("\n\n", bad.Select(b => b.ToString()));
        throw new InvalidOperationException(banner);
    }

    public void Log(ILogger logger)
    {
        foreach (var r in Run())
        {
            var text = r.ToString();
            switch (r.Severity)
            {
                case Severity.Info: logger.LogInformation("{Diagnostic}", text); break;
                case Severity.Warning: logger.LogWarning("{Diagnostic}", text); break;
                case Severity.Error: logger.LogError("{Diagnostic}", text); break;
                case Severity.Critical: logger.LogCritical("{Diagnostic}", text); break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }
    }

    private static IEnumerable<DiagnosticResult> Safe(Func<IEnumerable<DiagnosticResult>> f)
    {
        try
        {
            return f() ?? [];
        }
        catch (Exception ex)
        {
            return
            [
                new DiagnosticResult("HB000", Severity.Error, $"Verifier crashed: {ex.GetBaseException().Message}")
            ];
        }
    }
}