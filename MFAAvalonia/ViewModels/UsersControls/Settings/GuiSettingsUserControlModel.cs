using Avalonia.Collections;
using Avalonia.Styling;
using CommunityToolkit.Mvvm.ComponentModel;
using SukiUI;
using SukiUI.Models;

namespace MFAAvalonia.ViewModels.UsersControls.Settings;

public partial class GuiSettingsUserControlModel : ViewModelBase
{
    private readonly SukiTheme _theme = SukiTheme.GetInstance();

    [ObservableProperty] private bool _isLightTheme;
    
    public IAvaloniaReadOnlyList<SukiColorTheme> AvailableColors { get; }
    
    [ObservableProperty]  private SukiColorTheme _currentColorTheme;

    
    public GuiSettingsUserControlModel() 
    {
        IsLightTheme = _theme.ActiveBaseTheme == ThemeVariant.Light;
        _theme.OnBaseThemeChanged += variant =>
            IsLightTheme = variant == ThemeVariant.Light;
        AvailableColors = _theme.ColorThemes;
        _theme.OnColorThemeChanged += theme => CurrentColorTheme = theme;
    }
    
    partial void OnIsLightThemeChanged(bool value) =>
        _theme.ChangeBaseTheme(value ? ThemeVariant.Light : ThemeVariant.Dark);

}
