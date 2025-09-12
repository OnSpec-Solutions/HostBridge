using HostBridge.Diagnostics;
using Shouldly;
using Xunit;

namespace HostBridge.Diagnostics.Tests;

public class DiagnosticResultFormatTests
{
    [Fact]
    public void ToString_without_fix_does_not_include_fix_line()
    {
        var r = new DiagnosticResult("HB-TEST", Severity.Info, "Just info");
        var s = r.ToString();
        s.ShouldContain("HB-TEST");
        s.ShouldContain("Just info");
        s.ShouldNotContain("Fix:");
    }

    [Fact]
    public void ToString_with_fix_includes_fix_line()
    {
        var r = new DiagnosticResult("HB-TEST2", Severity.Warning, "Warn message", "Do the thing");
        var s = r.ToString();
        s.ShouldContain("HB-TEST2");
        s.ShouldContain("Warn message");
        s.ShouldContain("Fix: Do the thing");
    }
}
