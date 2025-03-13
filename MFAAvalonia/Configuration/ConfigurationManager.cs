using MFAAvalonia.Helper;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text.Json;

namespace MFAAvalonia.Configuration;

public class ConfigurationManager
{
    private static readonly IConfigurationRoot _configRoot;
    private static readonly string _configDir = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "MFAAvalonia/config");
    public static readonly Dictionary<string, object> Maa = new();
    
    
    static ConfigurationManager()
    {
        // 初始化配置系统
        var builder = new ConfigurationBuilder()
            .SetBasePath(_configDir)
            .AddJsonFile("config.json", optional: true)
            .AddJsonFile("maa_option.json", optional: true);

        _configRoot = builder.Build();
    }

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

    private static ObservableCollection<MFAConfiguration> LoadConfigurations()
    {
        var collection = new ObservableCollection<MFAConfiguration>();

        foreach (var file in Directory.EnumerateFiles(_configDir, "*.json"))
        {
            var fileName = Path.GetFileNameWithoutExtension(file);
            if (fileName == "maa_option") continue;
            var configs = JsonHelper.LoadConfig(fileName, new Dictionary<string, object>());
            
            var config = new MFAConfiguration
            {
                Name = fileName == "config" ? "Default" : fileName,
                FileName = fileName,
                Config = configs
            };

            collection.Add(config);
        }

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

