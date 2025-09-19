using System;
using System.IO;
using System.Xml.Linq;

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

        // appSettings are tolerant of empty values; load all keys that exist
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

        // connectionStrings can throw ConfigurationErrorsException if the file contains invalid entries
        // (e.g., an <add> with an empty name). We defensively ignore such errors so that consumers can still
        // access other configuration values. This mirrors legacy behavior where bad connectionStrings did not
        // block access to appSettings.
        try
        {
            var connectionStrings = ConfigurationManager.ConnectionStrings;
            if (connectionStrings != null)
            {
                foreach (ConnectionStringSettings setting in connectionStrings)
                {
                    if (string.IsNullOrEmpty(setting.Name))
                    {
                        continue;
                    }

                    data[$"connectionStrings:{setting.Name}"] = setting.ConnectionString;
                }
            }
        }
        catch (ConfigurationErrorsException)
        {
            // Swallow and proceed with whatever we could load (typically appSettings).
            // Attempt a best-effort manual parse of the current config file to extract connectionStrings.
            TryParseConnectionStringsFallback(data);
        }

        Data = data;
    }

    private static void TryParseConnectionStringsFallback(Dictionary<string, string?> data)
    {
        try
        {
#if NETFRAMEWORK
            var configPath = AppDomain.CurrentDomain.SetupInformation.ConfigurationFile;
            if (string.IsNullOrEmpty(configPath) || !File.Exists(configPath)) return;

            var doc = XDocument.Load(configPath);
            var cs = doc.Root?
                .Element("connectionStrings")?
                .Elements("add");
            if (cs == null) return;

            foreach (var add in cs)
            {
                var name = (string?)add.Attribute("name");
                var value = (string?)add.Attribute("connectionString");
                if (string.IsNullOrWhiteSpace(name)) continue;
                data[$"connectionStrings:{name}"] = value;
            }
#else
            // Not supported on non-NETFRAMEWORK TFMs; nothing to do.
#endif
        }
        catch
        {
            // Ignore any parse errors; this is a fallback best-effort path.
        }
    }
}