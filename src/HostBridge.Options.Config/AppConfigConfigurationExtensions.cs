namespace HostBridge.Options.Config;

/// <summary>
/// Extension methods to add HostBridge legacy app.config/web.config configuration.
/// </summary>
public static class AppConfigConfigurationExtensions
{
    /// <summary>
    /// Adds legacy app.config/web.config settings into the configuration pipeline.
    /// - appSettings: key => value
    /// - connectionStrings: "connectionStrings:{name}" => ConnectionString
    /// </summary>
    /// <param name="builder">The configuration builder to augment.</param>
    /// <returns>The same builder instance for chaining.</returns>
    /// <example>
    /// var cfg = new ConfigurationBuilder().AddHostBridgeAppConfig().Build();
    /// var cs = cfg["connectionStrings:Default"]; // reads ConnectionString from config
    /// </example>
    public static IConfigurationBuilder AddHostBridgeAppConfig(this IConfigurationBuilder builder)
        => builder.Add(new AppConfigConfigurationSource());
}