using Avalonia.Collections;
using Avalonia.Styling;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SukiUI;
using SukiUI.Enums;
using SukiUI.Models;
using System;

namespace MFAAvalonia.ViewModels.Pages;

public partial class SettingsViewModel : ViewModelBase
{
    private readonly SukiTheme _theme = SukiTheme.GetInstance();

    [ObservableProperty] private bool _backgroundAnimations;
    [ObservableProperty] private bool _backgroundTransitions;

    public string Name { get; set; } = "Settings";
    public Action<bool>? BackgroundAnimationsChanged { get; set; }
    public Action<bool>? BackgroundTransitionsChanged { get; set; }
    public SettingsViewModel()
    {

    }

    [RelayCommand]
    private void SwitchToColorTheme(SukiColorTheme colorTheme) =>
        _theme.ChangeColorTheme(colorTheme);

    partial void OnBackgroundAnimationsChanged(bool value) =>
        BackgroundAnimationsChanged?.Invoke(value);

    partial void OnBackgroundTransitionsChanged(bool value) =>
        BackgroundTransitionsChanged?.Invoke(value);
}
