namespace HostBridge.Options.Config;

/// <summary>
/// A configuration source that exposes values from legacy .NET app.config/web.config files.
/// </summary>
/// <remarks>
/// Provides key-value pairs from appSettings and connectionStrings sections via the HostBridge provider.
/// </remarks>
internal sealed class AppConfigConfigurationSource : IConfigurationSource
{
    /// <summary>
    /// Builds an <see cref="IConfigurationProvider"/> that reads values from app.config/web.config.
    /// </summary>
    /// <param name="builder">The configuration builder that is constructing the chain.</param>
    /// <returns>The provider instance.</returns>
    public IConfigurationProvider Build(IConfigurationBuilder builder) => new AppConfigConfigurationProvider();
}