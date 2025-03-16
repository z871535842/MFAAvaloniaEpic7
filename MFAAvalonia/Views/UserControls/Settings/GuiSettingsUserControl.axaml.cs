using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using MFAAvalonia.Helper;

namespace MFAAvalonia.Views.UserControls.Settings;

public partial class GuiSettingsUserControl : UserControl
{
    public GuiSettingsUserControl()
    {
        DataContext = Instances.GuiSettingsUserControlModel;
        InitializeComponent();
    }
}

