using HostBridge.Abstractions;

namespace HostBridge.AspNet;

/// <summary>
/// Bootstraps HostBridge for ASP.NET (System.Web) applications by exposing the root <see cref="IServiceProvider"/>.
/// </summary>
/// <remarks>
/// Call <see cref="Initialize"/> during Application_Start after building your <see cref="ILegacyHost"/>.
/// Combine with <see cref="AspNetRequestScopeModule"/> to get a per-request <see cref="Microsoft.Extensions.DependencyInjection.IServiceScope"/> so scoped
/// services behave like ASP.NET Core's RequestServices. If the module is not installed, resolution will fall back to
/// the root provider.
/// </remarks>
public static class AspNetBootstrapper
{
    /// <summary>
    /// Gets the root service provider for the application.
    /// </summary>
    public static IServiceProvider? RootServices { get; private set; }

    /// <summary>
    /// Initializes HostBridge for ASP.NET by capturing the root provider from the built host.
    /// </summary>
    /// <param name="host">The built legacy host.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="host"/> is null.</exception>
    public static void Initialize(ILegacyHost? host)
    {
        RootServices = host?.ServiceProvider ?? throw new ArgumentNullException(nameof(host));
        // Optional: attach Application_End to StopAsync if you want here, but you likely do that in Global.asax
    }
#if DEBUG
    // Test helper to reset static state; no-op in Release builds.
    internal static void _ResetForTests() => RootServices = null;
#endif
}