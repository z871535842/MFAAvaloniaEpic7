using MFAAvalonia.Helper;
using MFAWPF.Helper;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text.Json;

namespace MFAAvalonia.Configuration;

public static class ConfigurationManager
{
    private static readonly string _configDir = Path.Combine(
        AppContext.BaseDirectory,
        "config");
    public static readonly MFAConfiguration Maa = new("Maa", "maa_option", new Dictionary<string, object>());
    public static MFAConfiguration Current = new("Default", "config", new Dictionary<string, object>());
    

    public static ObservableCollection<MFAConfiguration> Configs { get; } = LoadConfigurations();

    public static int ConfigIndex { get; set; } = 0;

    public static string ConfigName { get; set; } = "Default";

    public static string GetCurrentConfiguration() => ConfigName;

    public static string GetActualConfiguration()
    {
        if (ConfigName.Equals("Default", StringComparison.OrdinalIgnoreCase))
            return "config";
        return GetCurrentConfiguration();
    }

    public static void Initialize()
    {
        LoggerHelper.Info("Current Configuration: " + GetCurrentConfiguration());
    }

    private static ObservableCollection<MFAConfiguration> LoadConfigurations()
    {
        LoggerHelper.Info("Loading Configurations...");
        var collection = new ObservableCollection<MFAConfiguration>();

        var defaultConfigPath = Path.Combine(_configDir, "config.json");
        if (!Directory.Exists(_configDir))
            Directory.CreateDirectory(_configDir);
        if (!File.Exists(defaultConfigPath))
            File.WriteAllText(defaultConfigPath, "{}");
        collection.Add(Current.SetConfig(JsonHelper.LoadConfig("config", new Dictionary<string, object>())));

        foreach (var file in Directory.EnumerateFiles(_configDir, "*.json"))
        {
            var fileName = Path.GetFileNameWithoutExtension(file);
            if (fileName == "maa_option" || fileName == "config") continue;
            var configs = JsonHelper.LoadConfig(fileName, new Dictionary<string, object>());

            var config = new MFAConfiguration(fileName, fileName, configs);

            collection.Add(config);
        }
        
        Maa.SetConfig(JsonHelper.LoadConfig("maa_option", new Dictionary<string, object>()));
        Current = collection.FirstOrDefault(c
                => !string.IsNullOrWhiteSpace(c.Name)
                && c.Name.Equals(ConfigName, StringComparison.OrdinalIgnoreCase))
            ?? Current;

        return collection;
    }

    public static void SaveConfiguration(string configName)
    {
        var config = Configs.FirstOrDefault(c => c.Name == configName);
        if (config != null)
        {
            JsonHelper.SaveConfig(config.FileName, config.Config);
        }
    }
}
