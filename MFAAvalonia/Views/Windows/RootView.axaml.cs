using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Controls.Notifications;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml.MarkupExtensions;
using Avalonia.Threading;
using Avalonia.VisualTree;
using MFAAvalonia.Configuration;
using MFAAvalonia.Extensions;
using MFAAvalonia.Extensions.MaaFW;
using MFAAvalonia.Helper;
using MFAAvalonia.Views.UserControls;
using SukiUI.Controls;
using SukiUI.Dialogs;
using SukiUI.Toasts;
using System;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Windows;

namespace MFAAvalonia.Views.Windows;

public partial class RootView : SukiWindow
{
    public RootView()
    {
        InitializeComponent();
        LanguageHelper.ChangeLanguage(LanguageHelper.SupportedLanguages[ConfigurationManager.Current.GetValue(ConfigurationKeys.LangIndex, 0)]);

    }


    protected override void OnClosing(WindowClosingEventArgs e)
    {
        e.Cancel = !ConfirmExit();
        ConfigurationManager.Current.SetValue(ConfigurationKeys.TaskItems, Instances.TaskQueueViewModel.TaskItemViewModels.ToList().Select(model => model.InterfaceItem));
        MaaProcessor.Instance.SetTasker();
        base.OnClosed(e);
    }


    public bool ConfirmExit()
    {
        if (!Instances.RootViewModel.IsRunning)
            return true;

        bool result = false;
        var frame = new DispatcherFrame();

        Instances.DialogManager.CreateDialog()
            .WithTitle("ConfirmExitTitle".ToLocalization())
            .WithContent("ConfirmExitText".ToLocalization()).OfType(NotificationType.Warning)
            .WithActionButton("Yes".ToLocalization(), _ =>
            {
                result = true;
                frame.Continue = false;
            }, dismissOnClick: true, "Flat", "Accent")
            .WithActionButton("No".ToLocalization(), _ =>
            {
                result = false;
                frame.Continue = false;
            }, dismissOnClick: true).TryShow();

        Dispatcher.UIThread.PushFrame(frame);
        return result;
    }

    private void ToggleWindowTopMost(object sender, RoutedEventArgs e)
    {
        Topmost = btnPin.IsChecked == true;
    }
}
