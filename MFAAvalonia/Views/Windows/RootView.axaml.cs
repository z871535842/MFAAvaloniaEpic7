using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml.MarkupExtensions;
using MFAAvalonia.Helper;
using SukiUI.Controls;
using System.Globalization;
using System.Windows;

namespace MFAAvalonia.Views.Windows;

public partial class RootView : SukiWindow
{
    public RootView()
    {
        InitializeComponent();
    }
    
    private void Chinese(object? sender, RoutedEventArgs e)
    {
        // I18NExtension.Culture = new CultureInfo("zh-cn");
        ToastNotification.Show("测试","无敌了无敌了");
    }
    
    private void English(object? sender, RoutedEventArgs e)
    {
        I18NExtension.Culture = new CultureInfo("en-us");
    }
}
