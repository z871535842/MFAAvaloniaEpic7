using CommunityToolkit.Mvvm.ComponentModel;
using MFAAvalonia.Extensions;
using MFAAvalonia.Helper;
using SukiUI.Dialogs;
using SukiUI.Toasts;
using System.Reflection;

namespace MFAAvalonia.ViewModels.Windows;

public partial class RootViewModel : ViewModelBase
{
#pragma warning disable CA1822 // Mark members as static
    [ObservableProperty] private string _greeting = "Welcome to Avalonia!";
#pragma warning restore CA1822 // Mark members as static

    [ObservableProperty] private bool _idle = true;

    [ObservableProperty] private bool _isRunning;
    [ObservableProperty] private bool _isConnected;
    public static string Version =>
        $"v{Assembly.GetExecutingAssembly().GetName().Version?.ToString() ?? "DEBUG"}";

    [ObservableProperty] private string? _resourceName;

    [ObservableProperty] private bool _isResourceNameVisible;

    [ObservableProperty] private string? _resourceVersion;

    [ObservableProperty] private string? _customTitle;

    [ObservableProperty] private bool _isCustomTitleVisible;


    [ObservableProperty] private string _title = "AppTitle".ToLocalization();

    public RootViewModel()
    {
        LanguageHelper.LanguageChanged += (sender, args) => Title = "AppTitle".ToLocalization();
    }

    public void ShowResourceName(string name)
    {
        ResourceName = name;
        IsResourceNameVisible = true;
    }

    public void ShowResourceVersion(string version)
    {
        ResourceVersion = version;
    }

    public void ShowCustomTitle(string title)
    {
        CustomTitle = title;
        IsCustomTitleVisible = true;
        IsResourceNameVisible = false;
    }
}
