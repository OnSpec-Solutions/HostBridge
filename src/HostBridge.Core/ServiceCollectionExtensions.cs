using HostBridge.Abstractions;

using Microsoft.Extensions.DependencyInjection;

namespace HostBridge.Core;

/// <summary>
/// Extension methods for registering HostBridge services.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Registers a hosted service as a singleton implementation of <see cref="IHostedService"/>.
    /// </summary>
    /// <typeparam name="THosted">The hosted service implementation type.</typeparam>
    /// <param name="services">The service collection.</param>
    /// <returns>The same <see cref="IServiceCollection"/> for chaining.</returns>
    /// <example>
    /// <code>services.AddHostedService&lt;HeartbeatService&gt;();</code>
    /// </example>
    public static IServiceCollection AddHostedService<THosted>(this IServiceCollection services)
        where THosted : class, IHostedService =>
        services.AddSingleton<IHostedService, THosted>();
}