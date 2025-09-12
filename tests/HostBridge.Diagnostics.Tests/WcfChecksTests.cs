using System;
using System.Collections.Generic;
using System.Linq;
using FluentAssertions;
using HostBridge.Abstractions;
using HostBridge.Diagnostics;
using HostBridge.Wcf;
using HostBridge.Core;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;
using Xunit;

namespace HostBridge.Diagnostics.Tests;

public class WcfChecksTests
{
    private static IReadOnlyList<DiagnosticResult> Run() => new HostBridgeVerifier().Add(WcfChecks.VerifyWcf).Run();

    private static void ResetWcfRoot()
    {
        var t = typeof(HostBridgeWcf);
        var f = t.GetField("<RootServices>k__BackingField", System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic);
        f!.SetValue(null, null);
    }

    private static void InitWcfRoot()
    {
        var host = new LegacyHostBuilder().ConfigureServices((_, s) => s.AddOptions()).ConfigureLogging(_ => { }).Build();
        HostBridgeWcf.Initialize(host);
    }

    [Fact]
    public void When_not_initialized_reports_critical_and_info()
    {
        ResetWcfRoot();
        var results = Run();
        results.Should().Contain(r => r.ToString().Contains("HB-WCF-001"));
        results.Should().Contain(r => r.ToString().Contains("HB-WCF-INFO"));
    }

    [Fact]
    public void When_initialized_reports_info_only()
    {
        InitWcfRoot();
        var results = Run();
        results.Should().NotContain(r => r.ToString().Contains("HB-WCF-001"));
        results.Should().Contain(r => r.ToString().Contains("HB-WCF-INFO"));
    }
}
