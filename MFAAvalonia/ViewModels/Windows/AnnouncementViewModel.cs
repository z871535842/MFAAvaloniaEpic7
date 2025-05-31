using CommunityToolkit.Mvvm.ComponentModel;
using MFAAvalonia.Configuration;
using MFAAvalonia.Helper;
using MFAAvalonia.Views.Windows;
using System;
using System.IO;

namespace MFAAvalonia.ViewModels.Windows;

public partial class AnnouncementViewModel : ViewModelBase
{
    public static readonly string AnnouncementFileName = "Announcement.md";
    public static readonly string ReleaseFileName = "Release.md";
    [ObservableProperty] private string _announcementInfo = string.Empty;

    [ObservableProperty] private bool _doNotRemindThisAnnouncementAgain = Convert.ToBoolean(GlobalConfiguration.GetValue(ConfigurationKeys.DoNotShowAgain, bool.FalseString));
    partial void OnDoNotRemindThisAnnouncementAgainChanged(bool value)
    {
        GlobalConfiguration.SetValue(ConfigurationKeys.DoNotShowAgain, value.ToString());
    }

    [ObservableProperty] private bool _isReleaseNote = false;

    public static void CheckReleaseNote()
    {
        var viewModel = new AnnouncementViewModel()
        {
            IsReleaseNote = true
        };
        try
        {
            var resourcePath = Path.Combine(AppContext.BaseDirectory, "resource");
            var mdPath = Path.Combine(resourcePath, ReleaseFileName);

            if (File.Exists(mdPath))
            {
                var content = File.ReadAllText(mdPath);
                viewModel.AnnouncementInfo = content;
            }
        }
        catch (Exception ex)
        {
            LoggerHelper.Error($"读取Release Note文件失败: {ex.Message}");
            viewModel.AnnouncementInfo = "";
        }
        finally
        {

            if (!string.IsNullOrWhiteSpace(viewModel.AnnouncementInfo) && !viewModel.AnnouncementInfo.Trim().Equals("placeholder", StringComparison.OrdinalIgnoreCase))
            {
                var announcementView = new AnnouncementView
                {
                    DataContext = viewModel
                };
                announcementView.Show();
            }
        }
    }

    public static void CheckAnnouncement()
    {
        var viewModel = new AnnouncementViewModel();
        if (viewModel.DoNotRemindThisAnnouncementAgain) return;
        try
        {
            var resourcePath = Path.Combine(AppContext.BaseDirectory, "resource");
            var mdPath = Path.Combine(resourcePath, AnnouncementFileName);

            if (File.Exists(mdPath))
            {
                var content = File.ReadAllText(mdPath);
                viewModel.AnnouncementInfo = content;
            }
        }
        catch (Exception ex)
        {
            LoggerHelper.Error($"读取公告文件失败: {ex.Message}");
            viewModel.AnnouncementInfo = "";
        }
        finally
        {

            if (!string.IsNullOrWhiteSpace(viewModel.AnnouncementInfo) && !viewModel.AnnouncementInfo.Trim().Equals("placeholder", StringComparison.OrdinalIgnoreCase))
            {
                var announcementView = new AnnouncementView
                {
                    DataContext = viewModel
                };
                announcementView.Show();
            }
        }
    }
}
