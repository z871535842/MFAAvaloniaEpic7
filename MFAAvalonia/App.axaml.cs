using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Data.Core;
using Avalonia.Data.Core.Plugins;
using Avalonia.Markup.Xaml;
using MFAAvalonia.Extensions;
using MFAAvalonia.Helper;
using MFAAvalonia.ViewModels;
using MFAAvalonia.ViewModels.Windows;
using MFAAvalonia.Views.Windows;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System;
using System.IO;

namespace MFAAvalonia;

public partial class App : Application
{
    private static readonly IHost Host = Microsoft.Extensions.Hosting.Host.CreateDefaultBuilder()
        .ConfigureAppConfiguration(c =>
        {
            var basePath =
                Path.GetDirectoryName(AppContext.BaseDirectory)
                ?? throw new DirectoryNotFoundException(
                    "Unable to find the base directory of the application."
                );
            _ = c.SetBasePath(basePath);
        })
        .ConfigureServices(
            (context, services) =>
            {
                // App Host


                // Main window with navigation
                _ = services.AddSingleton<RootView>();
                _ = services.AddSingleton(Instances.RootViewModel);
                // _ = services.AddSingleton<Views.Pages.DataPage>();
                // _ = services.AddSingleton<ViewModels.DataPageViewModel>();
                // _ = services.AddSingleton<Views.Pages.SettingsPage>();
                // _ = services.AddSingleton<ViewModels.SettingsViewModel>();

            }
        )
        .Build();

    /// <summary>
    /// Gets services.
    /// </summary>
    public static IServiceProvider Services => Host.Services;

    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);   
        LanguageHelper.Initialize();
    }


    public override void OnFrameworkInitializationCompleted()
    {
        var serviceProvider = Services;

        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            desktop.MainWindow = serviceProvider.GetRequiredService<RootView>();
            desktop.MainWindow.DataContext = serviceProvider.GetRequiredService<RootViewModel>();
        }
        Instances.RootViewModel.Greeting = "测试";
        Console.WriteLine(Instances.RootViewModel.Greeting);
        base.OnFrameworkInitializationCompleted();
    }
    
}
