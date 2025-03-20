using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using MFAAvalonia.Helper;

namespace MFAAvalonia.Views.UserControls.Settings;

public partial class ConfigurationMgrUserControl : UserControl
{
    public ConfigurationMgrUserControl()
    {
        DataContext = Instances.SettingsViewModel;
        InitializeComponent();
    }
}

