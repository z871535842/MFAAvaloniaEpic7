using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Avalonia.Styling;
using MFAAvalonia.Helper;
using MFAAvalonia.ViewModels.UsersControls.Settings;
using System;

namespace MFAAvalonia.Views.UserControls.Settings;

public partial class GuiSettingsUserControl : UserControl
{
    public GuiSettingsUserControl()
    {
        DataContext = Instances.GuiSettingsUserControlModel;
        InitializeComponent();
    }
}
