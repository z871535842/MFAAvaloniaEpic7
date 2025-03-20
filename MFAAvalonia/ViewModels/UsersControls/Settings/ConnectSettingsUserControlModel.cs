using CommunityToolkit.Mvvm.ComponentModel;
using MaaFramework.Binding;
using MFAAvalonia.Configuration;
using MFAAvalonia.Extensions.MaaFW;
using MFAAvalonia.Helper.Converters;
using MFAAvalonia.ViewModels.Other;
using System.Collections.ObjectModel;

namespace MFAAvalonia.ViewModels.UsersControls.Settings;

public partial class ConnectSettingsUserControlModel : ViewModelBase
{
    [ObservableProperty] private bool _rememberAdb = ConfigurationManager.Current.GetValue(ConfigurationKeys.RememberAdb, true);

    partial void OnRememberAdbChanged(bool value)
    {
        ConfigurationManager.Current.SetValue(ConfigurationKeys.RememberAdb, value);
    }

    public static ObservableCollection<AdbScreencapMethods> AdbControlScreenCapTypes =>
    [
        AdbScreencapMethods.Default, AdbScreencapMethods.RawWithGzip, AdbScreencapMethods.RawByNetcat, AdbScreencapMethods.Encode, AdbScreencapMethods.EncodeToFileAndPull, AdbScreencapMethods.MinicapDirect,
        AdbScreencapMethods.MinicapStream, AdbScreencapMethods.EmulatorExtras
    ];

    public static ObservableCollection<LocalizationViewModel> AdbControlInputTypes =>
    [
        new("MiniTouch")
        {
            Other = AdbInputMethods.MinitouchAndAdbKey
        },
        new("MaaTouch")
        {
            Other = AdbInputMethods.Maatouch
        },
        new("AdbInput")
        {
            Other = AdbInputMethods.AdbShell
        },
        new("EmulatorExtras")
        {
            Other = AdbInputMethods.EmulatorExtras
        },
        new("AutoDetect")
        {
            Other = AdbInputMethods.All
        }
    ];
    public static ObservableCollection<Win32ScreencapMethod> Win32ControlScreenCapTypes => [Win32ScreencapMethod.FramePool, Win32ScreencapMethod.DXGIDesktopDup, Win32ScreencapMethod.GDI];
    public static ObservableCollection<Win32InputMethod> Win32ControlInputTypes => [Win32InputMethod.SendMessage, Win32InputMethod.Seize];

    [ObservableProperty] private AdbScreencapMethods _adbControlScreenCapType =
        ConfigurationManager.Current.GetValue(ConfigurationKeys.AdbControlScreenCapType, AdbScreencapMethods.Default, AdbScreencapMethods.None, new UniversalEnumConverter<AdbScreencapMethods>());
    [ObservableProperty] private AdbInputMethods _adbControlInputType =
        ConfigurationManager.Current.GetValue(ConfigurationKeys.AdbControlInputType, AdbInputMethods.MinitouchAndAdbKey, AdbInputMethods.None, new UniversalEnumConverter<AdbInputMethods>());
    [ObservableProperty] private Win32ScreencapMethod _win32ControlScreenCapType =
        ConfigurationManager.Current.GetValue(ConfigurationKeys.Win32ControlScreenCapType, Win32ScreencapMethod.FramePool, Win32ScreencapMethod.None, new UniversalEnumConverter<Win32ScreencapMethod>());
    [ObservableProperty] private Win32InputMethod _win32ControlInputType =
        ConfigurationManager.Current.GetValue(ConfigurationKeys.Win32ControlInputType, Win32InputMethod.SendMessage, Win32InputMethod.None, new UniversalEnumConverter<Win32InputMethod>());

    partial void OnAdbControlScreenCapTypeChanged(AdbScreencapMethods value) => HandlePropertyChanged(ConfigurationKeys.AdbControlScreenCapType, value.ToString(), () => MaaProcessor.Instance.SetTasker());

    partial void OnAdbControlInputTypeChanged(AdbInputMethods value) => HandlePropertyChanged(ConfigurationKeys.AdbControlInputType, value.ToString(), () => MaaProcessor.Instance.SetTasker());

    partial void OnWin32ControlScreenCapTypeChanged(Win32ScreencapMethod value) => HandlePropertyChanged(ConfigurationKeys.Win32ControlScreenCapType, value.ToString(), () => MaaProcessor.Instance.SetTasker());

    partial void OnWin32ControlInputTypeChanged(Win32InputMethod value) => HandlePropertyChanged(ConfigurationKeys.Win32ControlInputType, value.ToString(), () => MaaProcessor.Instance.SetTasker());

    [ObservableProperty] private bool _retryOnDisconnected = ConfigurationManager.Current.GetValue(ConfigurationKeys.RetryOnDisconnected, false);

    partial void OnRetryOnDisconnectedChanged(bool value) => HandlePropertyChanged(ConfigurationKeys.RetryOnDisconnected, value);

    [ObservableProperty] private bool _allowAdbRestart = ConfigurationManager.Current.GetValue(ConfigurationKeys.AllowAdbRestart, true);

    partial void OnAllowAdbRestartChanged(bool value) => HandlePropertyChanged(ConfigurationKeys.AllowAdbRestart, value);


    [ObservableProperty] private bool _allowAdbHardRestart = ConfigurationManager.Current.GetValue(ConfigurationKeys.AllowAdbHardRestart, true);

    partial void OnAllowAdbHardRestartChanged(bool value) => HandlePropertyChanged(ConfigurationKeys.AllowAdbHardRestart, value);
}
