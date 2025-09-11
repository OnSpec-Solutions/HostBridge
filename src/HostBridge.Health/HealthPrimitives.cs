namespace HostBridge.Health;

/// <summary>
/// Represents the outcome of a health check.
/// </summary>
/// <param name="status">The overall health status.</param>
/// <param name="description">Optional human-readable description.</param>
/// <example>
/// var ok = HealthResult.Healthy("db ok");
/// var warn = HealthResult.Degraded("cache latency");
/// var fail = HealthResult.Unhealthy("dependency down");
/// </example>
public sealed class HealthResult(HealthStatus status, string? description = null)
{
    /// <summary>
    /// Gets the overall health status.
    /// </summary>
    public HealthStatus Status => status;

    /// <summary>
    /// Gets an optional description explaining the status.
    /// </summary>
    public string? Description => description;

    /// <summary>
    /// Creates a healthy result.
    /// </summary>
    public static HealthResult Healthy(string? description = null) => new(HealthStatus.Healthy, description);
    /// <summary>
    /// Creates a degraded result.
    /// </summary>
    public static HealthResult Degraded(string? description = null) => new(HealthStatus.Degraded, description);
    /// <summary>
    /// Creates an unhealthy result.
    /// </summary>
    public static HealthResult Unhealthy(string? description = null) => new(HealthStatus.Unhealthy, description);
}