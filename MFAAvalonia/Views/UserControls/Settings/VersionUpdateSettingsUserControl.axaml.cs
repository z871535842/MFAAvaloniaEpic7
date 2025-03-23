using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using MFAAvalonia.Helper;

namespace MFAAvalonia.Views.UserControls.Settings;

public partial class VersionUpdateSettingsUserControl : UserControl
{
    public VersionUpdateSettingsUserControl()
    {
        DataContext = Instances.VersionUpdateSettingsUserControlModel;
        InitializeComponent();
    }
}

