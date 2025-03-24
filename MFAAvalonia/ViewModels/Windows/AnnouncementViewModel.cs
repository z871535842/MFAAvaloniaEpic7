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
    [ObservableProperty] private string _announcementInfo = string.Empty;

    [ObservableProperty] private bool _doNotRemindThisAnnouncementAgain = Convert.ToBoolean(GlobalConfiguration.GetValue(ConfigurationKeys.DoNotShowAgain, bool.FalseString));
    partial void OnDoNotRemindThisAnnouncementAgainChanged(bool value)
    {
        GlobalConfiguration.SetValue(ConfigurationKeys.DoNotShowAgain, value.ToString());
    }


    public void CheckAnnouncement()
    {
        if (DoNotRemindThisAnnouncementAgain) return;
        try
        {
            var resourcePath = Path.Combine(AppContext.BaseDirectory, "resource");
            var mdPath = Path.Combine(resourcePath, AnnouncementFileName);

            if (File.Exists(mdPath))
            {
                var content = File.ReadAllText(mdPath);
                AnnouncementInfo = content;
            }
        }
        catch (Exception ex)
        {
            LoggerHelper.Error($"读取公告文件失败: {ex.Message}");
            AnnouncementInfo = "";
        }
        finally
        {

            if (!string.IsNullOrWhiteSpace(AnnouncementInfo) && !AnnouncementInfo.Trim().Equals("placeholder", StringComparison.OrdinalIgnoreCase))
            {
                var announcementView = new AnnouncementView();
                announcementView.Show();
            }
        }
    }
}
