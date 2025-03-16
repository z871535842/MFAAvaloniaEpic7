using MFAAvalonia.Utilities.Attributes;
using MFAAvalonia.ViewModels.Pages;
using MFAAvalonia.ViewModels.UsersControls.Settings;
using MFAAvalonia.ViewModels.Windows;
using MFAAvalonia.Views.Pages;
using MFAAvalonia.Views.UserControls.Settings;
using MFAAvalonia.Views.Windows;
using System;
using Microsoft.Extensions.DependencyInjection;
using SukiUI.Dialogs;
using SukiUI.Toasts;
using System.Collections.Concurrent;
using System.Threading;

namespace MFAAvalonia.Helper;

#pragma warning disable CS0169 // The field is never used
#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor

[LazyStatic]
public static partial class Instances
{
    #region Core Resolver

    private static readonly ConcurrentDictionary<Type, Lazy<object>> ServiceCache = new();

    /// <summary>
    /// 解析服务（自动缓存 + 循环依赖检测）
    /// </summary>
    private static T Resolve<T>()
    {
        var serviceType = typeof(T);
        var lazy = ServiceCache.GetOrAdd(serviceType, _ =>
            new Lazy<object>(
                () =>
                {
                    try { return App.Services.GetRequiredService<T>(); }
                    // try { return new T(); }
                    catch (InvalidOperationException ex)
                    {
                        throw new InvalidOperationException(
                            $"Failed to resolve service {typeof(T).Name}. Possible causes: 1. Service not registered; 2. Circular dependency detected; 3. Thread contention during initialization.", ex);
                    }
                },
                LazyThreadSafetyMode.ExecutionAndPublication
            ));
        return (T)lazy.Value;
    }

    #endregion    
    private static ISukiToastManager _toastManager;
    private static ISukiDialogManager _dialogManager;
    
    private static RootView _rootView;
    private static RootViewModel _rootViewModel;
    
    private static TaskQueueView _taskQueueView;
    private static TaskQueueViewModel _taskQueueViewModel;
    private static SettingsView _settingsView;
    private static SettingsViewModel _settingsViewModel;
    
    private static ConnectSettingsUserControl _connectSettingsUserControl;
    private static ConnectSettingsUserControlModel _connectSettingsUserControlModel;
    private static GuiSettingsUserControl _guiSettingsUser;
    private static GuiSettingsUserControlModel _guiSettingsUserControlModel;
}
