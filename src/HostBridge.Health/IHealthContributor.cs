using JetBrains.Annotations;

namespace HostBridge.Health;

/// <summary>
/// Defines a component that contributes to the application's overall health.
/// </summary>
/// <example>
/// public sealed class DbHealth : IHealthContributor {
///     public string Name => "Database";
///     public HealthResult Check() => HealthResult.Healthy();
/// }
/// </example>
[UsedImplicitly]
public interface IHealthContributor
{
    /// <summary>
    /// Gets the display name of this health contributor.
    /// </summary>
    [UsedImplicitly]
    string Name { get; }
        
    /// <summary>
    /// Runs the health check and returns the result.
    /// </summary>
    [UsedImplicitly]
    HealthResult Check();
}