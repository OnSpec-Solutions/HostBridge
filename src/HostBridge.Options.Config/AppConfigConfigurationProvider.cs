using System.Collections.Generic;
using System.Configuration;

using Microsoft.Extensions.Configuration;

using ConfigurationManager = System.Configuration.ConfigurationManager;

namespace HostBridge.Options.Config;

/// <summary>
/// A configuration provider that reads legacy appSettings and connectionStrings
/// from app.config/web.config and exposes them via the configuration system.
/// </summary>
internal sealed class AppConfigConfigurationProvider : ConfigurationProvider
{
    /// <summary>
    /// Loads configuration values from app.config/web.config into the provider's data dictionary.
    /// </summary>
    public override void Load()
    {
        var data = new Dictionary<string, string?>();

        var appSettings = ConfigurationManager.AppSettings;
        if (appSettings != null)
        {
            foreach (var key in appSettings.AllKeys)
            {
                if (key != null)
                {
                    data[key] = appSettings[key];
                }
            }
        }

        var connectionStrings = ConfigurationManager.ConnectionStrings;
        if (connectionStrings != null)
        {
            foreach (ConnectionStringSettings setting in connectionStrings)
            {
                if (string.IsNullOrEmpty(setting.Name) || setting.ElementInformation?.Source == null)
                {
                    continue;
                }

                data[$"connectionStrings:{setting.Name}"] = setting.ConnectionString;
            }
        }

        Data = data;
    }
}