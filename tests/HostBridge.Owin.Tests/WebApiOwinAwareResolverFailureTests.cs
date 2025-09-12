using System;
using System.Web;
using HostBridge.AspNet;
using HostBridge.Owin;
using Shouldly;
using Xunit;

namespace HostBridge.Owin.Tests;

[Collection("OwinNonParallel")]
public class WebApiOwinAwareResolverFailureTests
{
    private static void ResetAspNetRoot()
    {
        var t = typeof(AspNetBootstrapper);
        var f = t.GetField("<RootServices>k__BackingField", System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic);
        f!.SetValue(null, null);
    }

    [Fact]
    public void Given_no_root_initialized_When_resolving_Then_throws_InvalidOperationException()
    {
        // Ensure no HttpContext and no RootServices initialized
        HttpContext.Current = null;
        ResetAspNetRoot();

        var sut = new WebApiOwinAwareResolver();
        Should.Throw<InvalidOperationException>(() => sut.GetService(typeof(string)));
    }
}
