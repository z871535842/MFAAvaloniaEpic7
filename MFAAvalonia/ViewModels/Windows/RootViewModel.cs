using CommunityToolkit.Mvvm.ComponentModel;

namespace MFAAvalonia.ViewModels.Windows;

public partial class RootViewModel : ViewModelBase
{
#pragma warning disable CA1822 // Mark members as static
   [ObservableProperty] private string _greeting = "Welcome to Avalonia!";
#pragma warning restore CA1822 // Mark members as static
}
