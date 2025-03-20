using Avalonia.Collections;
using Avalonia.Styling;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MFAAvalonia.Configuration;
using MFAAvalonia.Extensions;
using MFAAvalonia.Extensions.MaaFW;
using MFAAvalonia.Helper;
using SukiUI;
using SukiUI.Enums;
using SukiUI.Models;
using System;
using System.IO;
using System.Linq;

namespace MFAAvalonia.ViewModels.Pages;

public partial class SettingsViewModel : ViewModelBase
{


    #region 配置

    public IAvaloniaReadOnlyList<MFAConfiguration> ConfigurationList { get; set; } = ConfigurationManager.Configs;

    [ObservableProperty] private string? _currentConfiguration = ConfigurationManager.GetCurrentConfiguration();

    partial void OnCurrentConfigurationChanged(string value)
    {
        ConfigurationManager.SetDefaultConfig(value);
        Instances.RestartApplication();
    }

    [ObservableProperty] private string _newConfigurationName = string.Empty;

    [RelayCommand]
    private void AddConfiguration()
    {
        if (string.IsNullOrWhiteSpace(NewConfigurationName))
        {
            NewConfigurationName = DateTime.Now.ToString("yyyyMMdd_HHmmss");
        }

        var configDPath = Path.Combine(AppContext.BaseDirectory, "config");
        var configPath = Path.Combine(configDPath, $"{ConfigurationManager.GetActualConfiguration()}.json");
        var newConfigPath = Path.Combine(configDPath, $"{NewConfigurationName}.json");
        bool configExists = Directory.GetFiles(configDPath, "*.json")
            .Select(Path.GetFileNameWithoutExtension)
            .Any(name => name.Equals(NewConfigurationName, StringComparison.OrdinalIgnoreCase));

        if (configExists)
        {
            ToastHelper.Error("ConfigNameAlreadyExists".ToLocalizationFormatted(false, NewConfigurationName));
            return;
        }
        if (File.Exists(configPath))
        {
            var content = File.ReadAllText(configPath);
            File.WriteAllText(newConfigPath, content);

            ConfigurationManager.Add(NewConfigurationName);
            ToastHelper.Success("ConfigAddedSuccessfully".ToLocalizationFormatted(false, NewConfigurationName));
        }
    }

    #endregion 配置
}
