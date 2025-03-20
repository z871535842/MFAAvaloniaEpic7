using Avalonia.Controls.ApplicationLifetimes;
using MFAAvalonia.Configuration;
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
using System.Diagnostics;
using System.Runtime.InteropServices;
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

    /// <summary>
    /// 关闭当前应用程序
    /// </summary>
    public static void ShutdownApplication()
    {
        ApplicationLifetime.MainWindow?.Close();
    }

    /// <summary>
    /// 重启当前应用程序
    /// </summary>
    public static void RestartApplication(bool noAutoStart = false)
    {
        if (noAutoStart)
            GlobalConfiguration.SetValue(ConfigurationKeys.NoAutoStart, bool.TrueString);
        var process = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = GetExecutablePath(),
                UseShellExecute = true
            }
        };

        try
        {
            process.Start();
            ShutdownApplication();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"重启失败: {ex.Message}");
        }
    }

    /// <summary>
    /// 关闭操作系统（需要管理员权限）
    /// </summary>
    public static void ShutdownSystem()
    {
        try
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                Process.Start("shutdown", "/s /t 0");
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                Process.Start("shutdown", "-h now");
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            {
                Process.Start("sudo", "shutdown -h now");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"关机失败: {ex.Message}");
        }
    }

    private static string GetExecutablePath()
    {
        // 兼容.NET 5+环境
        return Environment.ProcessPath ?? Process.GetCurrentProcess().MainModule?.FileName ?? AppContext.BaseDirectory;
    }

    private static IClassicDesktopStyleApplicationLifetime _applicationLifetime;
    private static ISukiToastManager _toastManager;
    private static ISukiDialogManager _dialogManager;

    private static RootView _rootView;
    private static RootViewModel _rootViewModel;

    private static TaskQueueView _taskQueueView;
    private static TaskQueueViewModel _taskQueueViewModel;
    private static SettingsView _settingsView;
    private static SettingsViewModel _settingsViewModel;
    private static ResourcesView _resourcesView;
    private static ResourcesViewModel _resourcesViewModel;
    
    private static ConnectSettingsUserControl _connectSettingsUserControl;
    private static ConnectSettingsUserControlModel _connectSettingsUserControlModel;
    private static GuiSettingsUserControl _guiSettingsUser;
    private static GuiSettingsUserControlModel _guiSettingsUserControlModel;
    private static ConfigurationMgrUserControl _configurationMgrUserControl;
    private static ExternalNotificationSettingsUserControl _externalNotificationSettingsUserControl;
    private static ExternalNotificationSettingsUserControlModel _externalNotificationSettingsUserControlModel;
    private static TimerSettingsUserControl _timerSettingsUserControl;
    private static TimerSettingsUserControlModel _timerSettingsUserControlModel;
    private static PerformanceUserControl _performanceUserControl;
    private static PerformanceUserControlModel _performanceUserControlModel;
    private static GameSettingsUserControl _gameSettingsUserControl;
    private static GameSettingsUserControlModel _gameSettingsUserControlModel;
}
