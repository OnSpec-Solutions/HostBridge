using System.IO;
using System.Reflection;
using System.Web;
using HostBridge.AspNet;
using HostBridge.Abstractions;
using HostBridge.Core;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;
using Xunit;

namespace HostBridge.AspNet.Tests;

public class AspNetRequestScopeModulePropertyInjectionEdgeTests
{
    private static void Invoke(string methodName)
    {
        var mi = typeof(AspNetRequestScopeModule)
            .GetMethod(methodName, BindingFlags.Static | BindingFlags.NonPublic);
        mi.ShouldNotBeNull();
        mi!.Invoke(null, null);
    }

    private static void BuildRoot(Action<IServiceCollection>? configure = null)
    {
        var host = new LegacyHostBuilder()
            .ConfigureLogging(_ => { })
            .ConfigureServices((_, s) =>
            {
                s.AddOptions();
                // Intentionally do NOT register UnregisteredDep
                s.AddSingleton<RegisteredDep>();
                configure?.Invoke(s);
            })
            .Build();
        AspNetBootstrapper.Initialize(host);
    }

    [Fact]
    public void OnPreRequestHandlerExecute_skips_properties_without_attribute_and_unresolvable_types()
    {
        BuildRoot();

        // Prepare HttpContext with a valid scope
        var req = new HttpRequest("test.aspx", "http://localhost/test.aspx", string.Empty);
        var resp = new HttpResponse(new StringWriter());
        var ctx = new HttpContext(req, resp);
        HttpContext.Current = ctx;
        var scope = AspNetBootstrapper.RootServices!.CreateScope();
        ctx.Items[Constants.ScopeKey] = scope;

        // Page with one property without [FromServices] and one with unregistered type
        var page = new TestPage();
        ctx.Handler = page;

        Invoke("OnPreRequestHandlerExecute");

        // Not decorated prop should remain null even though type is registered
        page.NoAttribute.ShouldBeNull();
        // Decorated but unregistered type remains null (no throw)
        page.Unregistered.ShouldBeNull();

        scope.Dispose();
    }

    private sealed class RegisteredDep { }
    private sealed class UnregisteredDep { }

    private sealed class TestPage : System.Web.UI.Page
    {
        public RegisteredDep? NoAttribute { get; set; }

        [FromServices]
        public UnregisteredDep? Unregistered { get; set; }
    }
}
