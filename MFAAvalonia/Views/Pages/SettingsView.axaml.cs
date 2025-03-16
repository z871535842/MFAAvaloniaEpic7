using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using MFAAvalonia.Helper;

namespace MFAAvalonia.Views.Pages;

public partial class SettingsView : UserControl
{
    public SettingsView()
    {
        DataContext = Instances.SettingsViewModel;
        InitializeComponent();
    }
}

