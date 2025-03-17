using CommunityToolkit.Mvvm.ComponentModel;
using MFAAvalonia.Configuration;
using System;

namespace MFAAvalonia.ViewModels;

public class ViewModelBase : ObservableObject
{
    protected ViewModelBase()
    {
        Initialize();
    }
    
    protected virtual void Initialize() { }
    
    protected void HandlePropertyChanged<T>(string configKey, T newValue, Action<T>? action = null)
    {
        ConfigurationManager.Current.SetValue(configKey, newValue);
        action?.Invoke(newValue);
    }
}
