using Avalonia.Collections;
using CommunityToolkit.Mvvm.ComponentModel;
using MaaFramework.Binding;
using MFAAvalonia.Configuration;
using MFAAvalonia.Helper.Converters;
using MFAAvalonia.ViewModels.Other;
using System.Collections.ObjectModel;

namespace MFAAvalonia.ViewModels.UsersControls.Settings;

public partial class PerformanceUserControlModel : ViewModelBase
{
    public static AvaloniaList<LocalizationViewModel> GpuOptions =>
    [
        new("GpuOptionAuto")
        {
            Other = InferenceDevice.Auto
        },
        new("GpuOptionDisable")
        {
            Other = InferenceDevice.CPU
        }
    ];

    [ObservableProperty] private InferenceDevice _gpuOption = ConfigurationManager.Current.GetValue(ConfigurationKeys.GPUOption, InferenceDevice.Auto, InferenceDevice.GPU0, new UniversalEnumConverter<InferenceDevice>());

    partial void OnGpuOptionChanged(InferenceDevice value) => HandlePropertyChanged(ConfigurationKeys.GPUOption, value.ToString());
}
