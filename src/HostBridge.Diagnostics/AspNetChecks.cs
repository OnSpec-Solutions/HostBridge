using System.Collections.Generic;

#if NET472_OR_GREATER
using System.Web;
using System.Web.Mvc;
using System.Web.Http;

using HostBridge.Abstractions;
using HostBridge.AspNet;

namespace HostBridge.Diagnostics;

public static class AspNetChecks
{
    public static IEnumerable<DiagnosticResult> VerifyAspNet()
    {
        // Root provider
        if (AspNetBootstrapper.RootServices is null)
            yield return new DiagnosticResult(
                "HB-ASP-001", Severity.Critical,
                "AspNetBootstrapper.Initialize(host) has not been called.",
                "Call AspNetBootstrapper.Initialize(host) in Global.asax Application_Start after building your ServiceProvider.");

        // IHttpModule registered & request scope functioning
        const string scopeKey = Constants.ScopeKey; // mirrors module
        if (HttpContext.Current != null)
        {
            // Simulate BeginRequest-installed items
            var hasScope = HttpContext.Current.Items.Contains(scopeKey);
            if (!hasScope)
                yield return new DiagnosticResult(
                    "HB-ASP-002", Severity.Error,
                    "AspNetRequestScopeModule is not creating a per-request scope.",
                    "Ensure web.config has <system.webServer><modules><add name=\"HostBridgeRequestScope\" type=\"HostBridge.AspNet.AspNetRequestScopeModule\"/></modules></system.webServer> and that the module is loading.");
        }
        else
        {
            yield return new DiagnosticResult(
                "HB-ASP-000", Severity.Info,
                "HttpContext.Current is null during verification (likely at startup). Skipping per-request scope check.");
        }

        // MVC resolver
        var current = DependencyResolver.Current?.GetType().FullName ?? "(null)";
        if (!current.Contains("HostBridge.Mvc5"))
            yield return new DiagnosticResult(
                "HB-MVC-001", Severity.Warning,
                "MVC5 DependencyResolver is not HostBridge’s resolver.",
                "Set DependencyResolver.SetResolver(new HostBridge.Mvc5.MvcDependencyResolver()) in Application_Start after AspNetBootstrapper.Initialize(host).");

        // Web API resolver
        var apiResolver = GlobalConfiguration.Configuration?.DependencyResolver?.GetType().FullName ?? "(null)";
        if (!apiResolver.Contains("HostBridge.WebApi2"))
            yield return new DiagnosticResult(
                "HB-WEBAPI2-001", Severity.Warning,
                "Web API 2 DependencyResolver is not HostBridge’s resolver.",
                "Set GlobalConfiguration.Configuration.DependencyResolver = new HostBridge.WebApi2.WebApiDependencyResolver().");
    }
}
#else
namespace HostBridge.Diagnostics;

public static class AspNetChecks
{
    public static IEnumerable<DiagnosticResult> VerifyAspNet()
    {
        yield break;
    }
}
#endif