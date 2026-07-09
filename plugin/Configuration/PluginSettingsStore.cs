using Newtonsoft.Json;
using revit_mcp_plugin.Utils;
using System.IO;

namespace revit_mcp_plugin.Configuration
{
    public static class PluginSettingsStore
    {
        public static ServiceSettings LoadSettings()
        {
            string path = PathManager.GetCommandRegistryFilePath();
            if (!File.Exists(path))
                return new ServiceSettings();

            try
            {
                var config = JsonConvert.DeserializeObject<FrameworkConfig>(File.ReadAllText(path));
                return config?.Settings ?? new ServiceSettings();
            }
            catch
            {
                return new ServiceSettings();
            }
        }

        public static void SaveSettings(ServiceSettings settings)
        {
            string path = PathManager.GetCommandRegistryFilePath();
            FrameworkConfig config;

            if (File.Exists(path))
            {
                config = JsonConvert.DeserializeObject<FrameworkConfig>(File.ReadAllText(path))
                    ?? new FrameworkConfig();
            }
            else
            {
                config = new FrameworkConfig();
            }

            config.Settings = settings;
            string json = JsonConvert.SerializeObject(config, Formatting.Indented);
            File.WriteAllText(path, json);
        }

        public static bool GetAutoStartOnLaunch()
        {
            return LoadSettings().AutoStartOnLaunch;
        }
    }
}
