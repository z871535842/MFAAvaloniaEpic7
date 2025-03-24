using Avalonia.Controls;
using MFAAvalonia.Configuration;
using MFAAvalonia.Extensions;
using MFAAvalonia.Extensions.MaaFW;
using MFAAvalonia.Helper;
using MFAAvalonia.Helper.Converters;
using MFAAvalonia.ViewModels.Windows;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using SukiUI.Dialogs;
using SukiUI.Toasts;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Net;
using System.Net.Http;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace MFAAvalonia.Helper;
#pragma warning  disable CS1998 // 此异步方法缺少 "await" 运算符，将以同步方式运行。
#pragma warning  disable CS4014 // 由于此调用不会等待，因此在此调用完成之前将会继续执行当前方法。
public static class VersionChecker
{
    private static readonly ConcurrentQueue<ValueType.MFATask> Queue = new();
    public static void Check()
    {
        var config = new
        {
            AutoUpdateResource = ConfigurationManager.Current.GetValue(ConfigurationKeys.EnableAutoUpdateResource, false),
            AutoUpdateMFA = ConfigurationManager.Current.GetValue(ConfigurationKeys.EnableAutoUpdateMFA, false),
            CheckVersion = ConfigurationManager.Current.GetValue(ConfigurationKeys.EnableCheckVersion, true),
        };

        if (config.AutoUpdateResource)
        {
            AddResourceUpdateTask(config.AutoUpdateMFA);
        }
        else if (config.CheckVersion)
        {
            AddResourceCheckTask();
        }

        if (config.AutoUpdateMFA)
        {
            AddMFAUpdateTask();
        }
        else if (config.CheckVersion)
        {
            AddMFACheckTask();
        }

        TaskManager.RunTaskAsync(async () => await ExecuteTasksAsync(),
            () => ToastNotification.Show("自动更新时发生错误！"), "启动检测");
    }
    public static void CheckMFAVersionAsync() => TaskManager.RunTaskAsync(() => CheckForMFAUpdates(Instances.VersionUpdateSettingsUserControlModel.DownloadSourceIndex == 0));
    public static void CheckResourceVersionAsync() => TaskManager.RunTaskAsync(() => CheckForResourceUpdates(Instances.VersionUpdateSettingsUserControlModel.DownloadSourceIndex == 0));
    public static void UpdateResourceAsync() => TaskManager.RunTaskAsync(() => UpdateResource(Instances.VersionUpdateSettingsUserControlModel.DownloadSourceIndex == 0));
    public static void UpdateMFAAsync() => TaskManager.RunTaskAsync(() => UpdateMFA(Instances.VersionUpdateSettingsUserControlModel.DownloadSourceIndex == 0));

    public static void UpdateMaaFwAsync() => TaskManager.RunTaskAsync(() => UpdateMaaFw());

    private static void AddResourceCheckTask()
    {
        Queue.Enqueue(new ValueType.MFATask
        {
            Action = async () => CheckForResourceUpdates(Instances.VersionUpdateSettingsUserControlModel.DownloadSourceIndex == 0),
            Name = "更新资源"
        });
    }

    private static void AddMFACheckTask()
    {
        Queue.Enqueue(new ValueType.MFATask
        {
            Action = async () => CheckForMFAUpdates(Instances.VersionUpdateSettingsUserControlModel.DownloadSourceIndex == 0),
            Name = "更新软件"
        });
    }

    private static void AddResourceUpdateTask(bool autoUpdateMFA)
    {
        Queue.Enqueue(new ValueType.MFATask
        {
            Action = async () => await UpdateResource(Instances.VersionUpdateSettingsUserControlModel.DownloadSourceIndex == 0, autoUpdateMFA, true),
            Name = "更新资源"
        });
    }

    private static SemaphoreSlim _queueLock = new(1, 1);

    private static void AddMFAUpdateTask()
    {
        Queue.Enqueue(new ValueType.MFATask
        {
            Action = async () => UpdateMFA(Instances.VersionUpdateSettingsUserControlModel.DownloadSourceIndex == 0),
            Name = "更新软件"
        });
    }

    public static void CheckForResourceUpdates(bool isGithub = true)
    {
        Instances.RootViewModel.SetUpdating(true);
        var url = MaaProcessor.Interface?.Url ?? string.Empty;

        string[] strings = [];
        try
        {
            if (isGithub)
            {
                if (string.IsNullOrWhiteSpace(url))
                {
                    ToastHelper.Info("CurrentResourcesNotSupportGitHub".ToLocalization());
                    Instances.RootViewModel.SetUpdating(false);
                    return;
                }
                strings = GetRepoFromUrl(url);
            }
            var resourceVersion = GetResourceVersion();
            if (string.IsNullOrWhiteSpace(resourceVersion))
            {
                Instances.RootViewModel.SetUpdating(false);
                return;
            }

            string latestVersion = string.Empty;
            if (isGithub)
                GetLatestVersionAndDownloadUrlFromGithub(out var downloadUrl, out latestVersion, strings[0], strings[1]);
            else
                GetDownloadUrlFromMirror(resourceVersion, GetResourceID(), CDK(), out _, out latestVersion, onlyCheck: true);

            if (string.IsNullOrWhiteSpace(latestVersion))
            {
                Instances.RootViewModel.SetUpdating(false);
                return;
            }

            if (IsNewVersionAvailable(latestVersion, resourceVersion))
            {
                DispatcherHelper.RunOnMainThread(() =>
                {
                    Instances.ToastManager.CreateToast().WithTitle("UpdateResource".ToLocalization())
                        .WithContent("ResourceOption".ToLocalization() + "NewVersionAvailableLatestVersion".ToLocalization() + latestVersion).Dismiss().After(TimeSpan.FromSeconds(6))
                        .WithActionButtonNormal("Later".ToLocalization(), _ => { }, true)
                        .WithActionButton("Update".ToLocalization(), _ => UpdateResourceAsync(), true).Queue();
                });
            }
            else
            {
                ToastHelper.Info("ResourcesAreLatestVersion".ToLocalization());
            }
            Instances.RootViewModel.SetUpdating(false);
        }
        catch (Exception ex)
        {
            Instances.RootViewModel.SetUpdating(false);
            if (ex.Message.Contains("resource not found"))
                ToastHelper.Error("CurrentResourcesNotSupportMirror".ToLocalization());
            else
                ToastHelper.Error("ErrorWhenCheck".ToLocalizationFormatted(true, "Resource"), ex.Message);
            LoggerHelper.Error(ex);
        }
    }

    public static void CheckForMFAUpdates(bool isGithub = true)
    {
        try
        {
            Instances.RootViewModel.SetUpdating(true);
            var localVersion = GetLocalVersion();
            string latestVersion = string.Empty;
            if (isGithub)
                GetLatestVersionAndDownloadUrlFromGithub(out var downloadUrl, out latestVersion);
            else
                GetDownloadUrlFromMirror(localVersion, "MFAAvalonia", CDK(), out _, out latestVersion, isUI: true, onlyCheck: true);

            if (IsNewVersionAvailable(latestVersion, localVersion))
            {
                DispatcherHelper.RunOnMainThread(() =>
                {
                    Instances.ToastManager.CreateToast().WithTitle("SoftwareUpdate".ToLocalization())
                        .WithTitle("MFA" + "NewVersionAvailableLatestVersion".ToLocalization() + latestVersion).Dismiss().After(TimeSpan.FromSeconds(6))
                        .WithActionButtonNormal("Later".ToLocalization(), _ => { }, true)
                        .WithActionButton("Update".ToLocalization(), _ => UpdateMFAAsync(), true).Queue();
                });
            }
            else
            {
                ToastHelper.Info("MFAIsLatestVersion".ToLocalization());
            }

            Instances.RootViewModel.SetUpdating(false);
        }
        catch (Exception ex)
        {
            if (ex.Message.Contains("resource not found"))
                ToastHelper.Error("CurrentResourcesNotSupportMirror".ToLocalization());
            else
                ToastHelper.Error("ErrorWhenCheck".ToLocalizationFormatted(false, "MFA"), ex.Message);
            Instances.RootViewModel.SetUpdating(false);
            LoggerHelper.Error(ex);
        }
    }

    public async static Task UpdateResource(bool isGithub = true, bool closeDialog = false, bool noDialog = false, Action action = null)
    {
        Instances.RootViewModel.SetUpdating(true);
        ProgressBar? progress = null;
        TextBlock? textBlock = null;
        ISukiToast? sukiToast = null;
        DispatcherHelper.RunOnMainThread(() =>
        {
            progress = new ProgressBar
            {
                Value = 0,
                ShowProgressText = true
            };
            StackPanel stackPanel = new();
            var textBlock = new TextBlock()
            {
                Text = "GettingLatestResources".ToLocalization(),
            };
            stackPanel.Children.Add(textBlock);
            stackPanel.Children.Add(progress);
            sukiToast = Instances.ToastManager.CreateToast()
                .WithTitle("UpdateResource".ToLocalization())
                .WithContent(stackPanel).Queue();
        });


        var localVersion = MaaProcessor.Interface?.Version ?? string.Empty;

        if (string.IsNullOrWhiteSpace(localVersion))
        {
            Dismiss(sukiToast);
            ToastHelper.Warn("FailToGetCurrentVersionInfo".ToLocalization());
            Instances.RootViewModel.SetUpdating(false);
            return;
        }
        SetProgress(progress, 10);
        string[] strings = [];
        if (isGithub)
        {
            var url = MaaProcessor.Interface?.Url ?? string.Empty;
            if (string.IsNullOrWhiteSpace(url))
            {
                Dismiss(sukiToast);
                ToastHelper.Warn("CurrentResourcesNotSupportGitHub".ToLocalization());
                Instances.RootViewModel.SetUpdating(false);
                return;
            }
            strings = GetRepoFromUrl(url);
        }
        string latestVersion = string.Empty;
        string downloadUrl = string.Empty;
        try
        {
            if (isGithub)
                GetLatestVersionAndDownloadUrlFromGithub(out downloadUrl, out latestVersion, strings[0], strings[1]);
            else
                GetDownloadUrlFromMirror(localVersion, GetResourceID(), CDK(), out downloadUrl, out latestVersion);

        }
        catch (Exception ex)
        {
            Dismiss(sukiToast);
            ToastHelper.Warn($"{"FailToGetLatestVersionInfo".ToLocalization()}: {ex.Message}");
            Instances.RootViewModel.SetUpdating(false);
            LoggerHelper.Error(ex);

            return;
        }

        SetProgress(progress, 50);

        if (string.IsNullOrWhiteSpace(latestVersion))
        {
            Dismiss(sukiToast);
            ToastHelper.Warn("FailToGetLatestVersionInfo".ToLocalization());
            Instances.RootViewModel.SetUpdating(false);
            Instances.TaskQueueViewModel.ClearDownloadProgress();

            return;
        }

        if (!IsNewVersionAvailable(latestVersion, localVersion))
        {
            Dismiss(sukiToast);
            ToastHelper.Info("ResourcesAreLatestVersion".ToLocalization());
            Instances.RootViewModel.SetUpdating(false);
            Instances.TaskQueueViewModel.ClearDownloadProgress();
            action?.Invoke();
            return;
        }

        SetProgress(progress, 100);

        if (string.IsNullOrWhiteSpace(downloadUrl))
        {
            Dismiss(sukiToast);
            ToastHelper.Warn("FailToGetDownloadUrl".ToLocalization());
            Instances.RootViewModel.SetUpdating(false);
            Instances.TaskQueueViewModel.ClearDownloadProgress();
            return;
        }

        var tempPath = Path.Combine(AppContext.BaseDirectory, "temp_res");
        Directory.CreateDirectory(tempPath);

        var tempZipFilePath = Path.Combine(tempPath, $"resource_{latestVersion}.zip");
        SetText(textBlock, "Downloading".ToLocalization());
        SetProgress(progress, 0);
        if (!await DownloadFileAsync(downloadUrl, tempZipFilePath, progress, "GameResourceUpdated"))
        {
            Dismiss(sukiToast);
            ToastHelper.Warn("DownloadFailed".ToLocalization());
            return;
        }

        SetText(textBlock, "ApplyingUpdate".ToLocalization());
        SetProgress(progress, 0);

        var tempExtractDir = Path.Combine(tempPath, $"resource_{latestVersion}_extracted");
        if (Directory.Exists(tempExtractDir)) Directory.Delete(tempExtractDir, true);
        if (!File.Exists(tempZipFilePath))
        {
            Dismiss(sukiToast);
            ToastHelper.Warn("DownloadFailed".ToLocalization());
            return;
        }

        ZipFile.ExtractToDirectory(tempZipFilePath, tempExtractDir);
        SetProgress(progress, 50);
        var resourcePath = Path.Combine(AppContext.BaseDirectory, "resource");
        if (Directory.Exists(resourcePath))
        {
            foreach (var rfile in Directory.EnumerateFiles(resourcePath, "*", SearchOption.AllDirectories))
            {
                var fileName = Path.GetFileName(rfile);
                if (fileName.Equals(AnnouncementViewModel.AnnouncementFileName, StringComparison.OrdinalIgnoreCase))
                    continue;

                try
                {
                    File.SetAttributes(rfile, FileAttributes.Normal);
                    File.Delete(rfile);
                }
                catch (Exception ex)
                {
                    LoggerHelper.Warning($"文件删除失败: {rfile} - {ex.Message}");
                }
            }
        }

        var interfacePath = Path.Combine(tempExtractDir, "interface.json");
        var resourceDirPath = Path.Combine(tempExtractDir, "resource");

        string wpfDir = AppContext.BaseDirectory;
        var file = new FileInfo(interfacePath);
        if (file.Exists)
        {
            var targetPath = Path.Combine(wpfDir, "interface.json");
            file.CopyTo(targetPath, true);
        }

        SetProgress(progress, 60);

        var di = new DirectoryInfo(resourceDirPath);
        if (di.Exists)
        {
            CopyFolder(resourceDirPath, Path.Combine(wpfDir, "resource"));
        }

        SetProgress(progress, 70);

        File.Delete(tempZipFilePath);
        Directory.Delete(tempExtractDir, true);
        SetProgress(progress, 80);

        var newInterfacePath = Path.Combine(wpfDir, "interface.json");
        if (File.Exists(newInterfacePath))
        {
            var jsonContent = await File.ReadAllTextAsync(newInterfacePath);
            var settings = new JsonSerializerSettings
            {
                Formatting = Formatting.Indented,
                NullValueHandling = NullValueHandling.Ignore,
                DefaultValueHandling = DefaultValueHandling.Ignore
            };

            settings.Converters.Add(new MaaInterfaceSelectOptionConverter(true));

            var @interface = JsonConvert.DeserializeObject<MaaInterface>(jsonContent, settings);
            if (@interface != null)
            {
                @interface.Url = MaaProcessor.Interface?.Url;
                @interface.Version = latestVersion;
            }
            var updatedJsonContent = JsonConvert.SerializeObject(@interface, settings);

            await File.WriteAllTextAsync(newInterfacePath, updatedJsonContent);
        }

        SetProgress(progress, 100);

        SetText(textBlock, "UpdateCompleted".ToLocalization());
        // dialog?.SetRestartButtonVisibility(true);

        Instances.RootViewModel.SetUpdating(false);

        DispatcherHelper.RunOnMainThread(() =>
        {
            if (!noDialog)
            {
                Instances.DialogManager.CreateDialog().WithContent("GameResourceUpdated".ToLocalization()).WithActionButton("Yes".ToLocalization(), _ =>
                    {
                        Process.Start(Process.GetCurrentProcess().MainModule?.FileName ?? string.Empty);
                        Instances.ShutdownApplication();
                        Instances.ApplicationLifetime.Shutdown();
                    }, dismissOnClick: true, "Flat", "Accent")
                    .WithActionButton("No".ToLocalization(), _ =>
                    {
                    }, dismissOnClick: true).TryShow();
            }
        });

        var tasks = Instances.TaskQueueViewModel.TaskItemViewModels;
        Instances.RootView.ClearTasks(() => MaaProcessor.Instance.InitializeData(dragItem: tasks));
        action?.Invoke();
    }

    public async static Task UpdateMFA(bool isGithub, bool noDialog = false)
    {
        Instances.RootViewModel.SetUpdating(true);
        ProgressBar? progress = null;
        TextBlock? textBlock = null;
        ISukiToast? sukiToast = null;
        DispatcherHelper.RunOnMainThread(() =>
        {
            progress = new ProgressBar
            {
                Value = 0,
                ShowProgressText = true
            };
            StackPanel stackPanel = new();
            var textBlock = new TextBlock()
            {
                Text = "GettingLatestSoftware".ToLocalization(),
            };
            stackPanel.Children.Add(textBlock);
            stackPanel.Children.Add(progress);
            sukiToast = Instances.ToastManager.CreateToast()
                .WithTitle("SoftwareUpdate".ToLocalization())
                .WithContent(stackPanel).Queue();
        });


        SetProgress(progress, 10);

        string downloadUrl = string.Empty, latestVersion = string.Empty;
        try
        {
            if (isGithub)
                GetLatestVersionAndDownloadUrlFromGithub(out downloadUrl, out latestVersion);
            else
                GetDownloadUrlFromMirror(GetLocalVersion(), "MFAAvalonia", CDK(), out downloadUrl, out latestVersion);
        }
        catch (Exception ex)
        {
            Dismiss(sukiToast);
            ToastHelper.Warn($"{"FailToGetLatestVersionInfo".ToLocalization()}: {ex.Message}");
            Instances.RootViewModel.SetUpdating(false);
            LoggerHelper.Error(ex);
            return;
        }

        SetProgress(progress, 50);

        if (string.IsNullOrWhiteSpace(latestVersion))
        {
            Dismiss(sukiToast);
            ToastHelper.Warn("FailToGetLatestVersionInfo".ToLocalization());
            Instances.RootViewModel.SetUpdating(false);
            Instances.TaskQueueViewModel.ClearDownloadProgress();
            return;
        }

        if (!IsNewVersionAvailable(latestVersion, GetLocalVersion()))
        {
            Dismiss(sukiToast);
            ToastHelper.Info("MFAIsLatestVersion".ToLocalization());
            Instances.RootViewModel.SetUpdating(false);
            Instances.TaskQueueViewModel.ClearDownloadProgress();
            return;
        }

        SetProgress(progress, 100);

        if (string.IsNullOrWhiteSpace(downloadUrl))
        {
            Dismiss(sukiToast);
            ToastHelper.Warn("FailToGetDownloadUrl".ToLocalization());
            Instances.RootViewModel.SetUpdating(false);
            Instances.TaskQueueViewModel.ClearDownloadProgress();
            return;
        }

        var tempPath = Path.Combine(AppContext.BaseDirectory, "temp_mfa");
        Directory.CreateDirectory(tempPath);

        var tempZipFilePath = Path.Combine(tempPath, $"mfa_{latestVersion}.zip");
        SetText(textBlock, "Downloading".ToLocalization());
        SetProgress(progress, 0);

        if (!await DownloadFileAsync(downloadUrl, tempZipFilePath, progress, "GameResourceUpdated"))
        {
            Dismiss(sukiToast);
            ToastHelper.Warn("DownloadFailed");
            Instances.TaskQueueViewModel.ClearDownloadProgress();
            return;
        }

        var tempExtractDir = Path.Combine(tempPath, $"mfa_{latestVersion}_extracted");
        if (Directory.Exists(tempExtractDir)) Directory.Delete(tempExtractDir, true);
        if (!File.Exists(tempZipFilePath))
        {
            Dismiss(sukiToast);
            ToastHelper.Warn("DownloadFailed");
            Instances.TaskQueueViewModel.ClearDownloadProgress();
            return;
        }

        ZipFile.ExtractToDirectory(tempZipFilePath, tempExtractDir);

        var currentExeFileName = Process.GetCurrentProcess().MainModule.ModuleName;

        var utf8Bytes = Encoding.UTF8.GetBytes(AppContext.BaseDirectory);
        var utf8BaseDirectory = Encoding.UTF8.GetString(utf8Bytes);
        var extractedPath = $"\"{utf8BaseDirectory}temp_mfa\\mfa_{latestVersion}_extracted\\*.*\"";
        var extracted = $"{utf8BaseDirectory}temp_mfa\\mfa_{latestVersion}_extracted\\";
        var scriptFileName = RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? "update_mfa.bat" : "update_mfa.sh";
        var scriptFilePath = Path.Combine(tempPath, scriptFileName);

        await using (var sw = new StreamWriter(scriptFilePath))
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                await sw.WriteLineAsync("@echo off");
                await sw.WriteLineAsync("chcp 65001");
                await sw.WriteLineAsync("ping 127.0.0.1 -n 3 > nul");
                await sw.WriteLineAsync($"copy /Y \"{extracted}{Assembly.GetEntryAssembly().GetName().Name}.exe\" \"{AppContext.BaseDirectory}{currentExeFileName}\"");
                await sw.WriteLineAsync("ping 127.0.0.1 -n 1 > nul");
                await sw.WriteLineAsync($"del \"{extracted}{Assembly.GetEntryAssembly().GetName().Name}.exe\"");
                await sw.WriteLineAsync("ping 127.0.0.1 -n 1 > nul");
                await sw.WriteLineAsync($"xcopy /E /Y {extractedPath} {AppContext.BaseDirectory}");
                await sw.WriteLineAsync("ping 127.0.0.1 -n 1 > nul");
                await sw.WriteLineAsync($"start /d \"{AppContext.BaseDirectory}\" {currentExeFileName}");
                await sw.WriteLineAsync($"rd /S /Q \"{tempPath}\"");
            }
            else
            {
                await sw.WriteLineAsync("#!/bin/bash");
                await sw.WriteLineAsync("sleep 3");
                await sw.WriteLineAsync($"cp -f \"{extracted}{Assembly.GetEntryAssembly().GetName().Name}.exe\" \"{AppContext.BaseDirectory}{currentExeFileName}\"");
                await sw.WriteLineAsync("sleep 1");
                await sw.WriteLineAsync($"rm -f \"{extracted}{Assembly.GetEntryAssembly().GetName().Name}.exe\"");
                await sw.WriteLineAsync("sleep 1");
                await sw.WriteLineAsync($"cp -r {extractedPath} {AppContext.BaseDirectory}");
                await sw.WriteLineAsync("sleep 1");
                await sw.WriteLineAsync($"\"{AppContext.BaseDirectory}{currentExeFileName}\"");
                await sw.WriteLineAsync($"rm -rf \"{tempPath}\"");
            }
        }

        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            Process.Start("chmod", $"+x \"{scriptFilePath}\"");
        }

        var psi = new ProcessStartInfo(scriptFilePath)
        {
            CreateNoWindow = true,
            WindowStyle = ProcessWindowStyle.Hidden
        };
        Process.Start(psi);
        Thread.Sleep(50);
        Instances.ShutdownApplication();
    }
    public async static Task UpdateMaaFw()
    {
        Instances.RootViewModel.SetUpdating(true);

        ProgressBar? progress = null;
        TextBlock? textBlock = null;
        ISukiToast? sukiToast = null;
        DispatcherHelper.RunOnMainThread(() =>
        {
            progress = new ProgressBar
            {
                Value = 0,
                ShowProgressText = true
            };
            StackPanel stackPanel = new();
            var textBlock = new TextBlock()
            {
                Text = "GettingLatestMaaFW".ToLocalization(),
            };
            stackPanel.Children.Add(textBlock);
            stackPanel.Children.Add(progress);
            sukiToast = Instances.ToastManager.CreateToast()
                .WithTitle("UpdateMaaFW".ToLocalization())
                .WithContent(stackPanel).Queue();
        });

        SetProgress(progress, 10);

        var resId = "MaaFramework";
        var currentVersion = MaaProcessor.Utility.Version;
        Instances.RootViewModel.SetUpdating(true);
        string downloadUrl = string.Empty, latestVersion = string.Empty;
        try
        {
            GetDownloadUrlFromMirror(currentVersion, resId, CDK(), out downloadUrl, out latestVersion, "MFA", true);
        }
        catch (Exception ex)
        {
            Dismiss(sukiToast);
            ToastHelper.Warn($"{"FailToGetLatestVersionInfo".ToLocalization()}: {ex.Message}");
            Instances.RootViewModel.SetUpdating(false);
            LoggerHelper.Error(ex);
            return;
        }

        SetProgress(progress, 50);

        if (string.IsNullOrWhiteSpace(latestVersion))
        {
            Dismiss(sukiToast);
            ToastHelper.Warn("FailToGetLatestVersionInfo".ToLocalization());
            Instances.RootViewModel.SetUpdating(false);

            Instances.TaskQueueViewModel.ClearDownloadProgress();
            return;
        }


        if (string.IsNullOrWhiteSpace(currentVersion))
        {
            Dismiss(sukiToast);
            ToastHelper.Warn("FailToGetCurrentVersionInfo".ToLocalization());
            Instances.RootViewModel.SetUpdating(false);

            Instances.TaskQueueViewModel.ClearDownloadProgress();
            return;
        }

        LoggerHelper.Info($"latestVersion, localVersion: {latestVersion}, {currentVersion}");
        if (!IsNewVersionAvailable(latestVersion, currentVersion))
        {
            Dismiss(sukiToast);

            ToastHelper.Info("MaaFwIsLatestVersion".ToLocalization());
            Instances.RootViewModel.SetUpdating(false);

            Instances.TaskQueueViewModel.ClearDownloadProgress();
            return;
        }

        SetProgress(progress, 100);

        if (string.IsNullOrWhiteSpace(downloadUrl))
        {
            Dismiss(sukiToast);
            ToastHelper.Warn("FailToGetDownloadUrl".ToLocalization());
            Instances.RootViewModel.SetUpdating(false);

            Instances.TaskQueueViewModel.ClearDownloadProgress();
            return;
        }

        var tempPath = Path.Combine(AppContext.BaseDirectory, "temp_maafw");
        Directory.CreateDirectory(tempPath);

        var tempZipFilePath = Path.Combine(tempPath, $"maafw_{latestVersion}.zip");
        SetText(textBlock, "Downloading".ToLocalization());
        SetProgress(progress, 0);

        if (!await DownloadFileAsync(downloadUrl, tempZipFilePath, progress, "GameResourceUpdated"))
        {
            Dismiss(sukiToast);
            ToastHelper.Warn("DownloadFailed");
            Instances.TaskQueueViewModel.ClearDownloadProgress();
            return;
        }

        var tempExtractDir = Path.Combine(tempPath, $"maafw_{latestVersion}_extracted");
        if (Directory.Exists(tempExtractDir)) Directory.Delete(tempExtractDir, true);
        if (!File.Exists(tempZipFilePath))
        {
            Dismiss(sukiToast);
            ToastHelper.Warn("DownloadFailed");
            Instances.TaskQueueViewModel.ClearDownloadProgress();
            return;
        }

        ZipFile.ExtractToDirectory(tempZipFilePath, tempExtractDir);

        var currentExeFileName = Process.GetCurrentProcess().MainModule.ModuleName;

        var utf8Bytes = Encoding.UTF8.GetBytes(AppContext.BaseDirectory);
        var utf8BaseDirectory = Encoding.UTF8.GetString(utf8Bytes);
        var scriptFileName = RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? "update_maafw.bat" : "update_maafw.sh";
        var scriptFilePath = Path.Combine(utf8BaseDirectory, "temp_maafw", scriptFileName);

        await using (var sw = new StreamWriter(scriptFilePath))
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                await sw.WriteLineAsync("@echo off");
                await sw.WriteLineAsync("chcp 65001");
                await sw.WriteLineAsync("ping 127.0.0.1 -n 3 > nul");
                var extractedPath = $"\"{utf8BaseDirectory}temp_maafw\\maafw_{latestVersion}_extracted\\bin\\*.*\"";
                var targetPath = $"\"{utf8BaseDirectory}\"";
                await sw.WriteLineAsync("ping 127.0.0.1 -n 1 > nul");
                await sw.WriteLineAsync($"xcopy /E /Y {extractedPath} {targetPath}");
                await sw.WriteLineAsync("ping 127.0.0.1 -n 1 > nul");
                await sw.WriteLineAsync($"start /d \"{utf8BaseDirectory}\" {currentExeFileName}");
                await sw.WriteLineAsync($"rd /S /Q \"{utf8BaseDirectory}temp_maafw\"");
            }
            else
            {
                await sw.WriteLineAsync("#!/bin/bash");
                await sw.WriteLineAsync("sleep 3");
                var extractedPath = $"\"{utf8BaseDirectory}temp_maafw/maafw_{latestVersion}_extracted/bin/*\"";
                var targetPath = $"\"{utf8BaseDirectory}\"";
                await sw.WriteLineAsync("sleep 1");
                await sw.WriteLineAsync($"cp -r {extractedPath} {targetPath}");
                await sw.WriteLineAsync("sleep 1");
                await sw.WriteLineAsync($"\"{utf8BaseDirectory}{currentExeFileName}\"");
                await sw.WriteLineAsync($"rm -rf \"{utf8BaseDirectory}temp_maafw\"");
            }
        }

        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            Process.Start("chmod", $"+x \"{scriptFilePath}\"");
        }

        var psi = new ProcessStartInfo(scriptFilePath)
        {
            CreateNoWindow = true,
            WindowStyle = ProcessWindowStyle.Hidden
        };
        Process.Start(psi);
        Thread.Sleep(50);
        Instances.ShutdownApplication();
    }
    async private static Task ExecuteTasksAsync()
    {
        try
        {
            while (Queue.TryDequeue(out var task))
            {
                await _queueLock.WaitAsync();
                LoggerHelper.Info($"开始执行任务: {task.Name}");
                await task.Action();
                LoggerHelper.Info($"任务完成: {task.Name}");
                _queueLock.Release();
            }
        }
        finally
        {
            Instances.RootViewModel.SetUpdating(false);
        }
    }


    public static void GetLatestVersionAndDownloadUrlFromGithub(out string url, out string latestVersion, string owner = "SweetSmellFox", string repo = "MFAAvalonia")
    {
        url = string.Empty;
        latestVersion = string.Empty;

        if (string.IsNullOrWhiteSpace(owner) || string.IsNullOrWhiteSpace(repo))
            return;

        var releaseUrl = $"https://api.github.com/repos/{owner}/{repo}/releases";
        int page = 1;
        const int perPage = 5;
        using var httpClient = new HttpClient();
        httpClient.DefaultRequestHeaders.UserAgent.TryParseAdd("request");
        httpClient.DefaultRequestHeaders.Accept.TryParseAdd("application/json");

        while (page < 101)
        {
            var urlWithParams = $"{releaseUrl}?per_page={perPage}&page={page}";
            try
            {
                var response = httpClient.GetAsync(urlWithParams).Result;
                if (response.IsSuccessStatusCode)
                {
                    var read = response.Content.ReadAsStringAsync();
                    read.Wait();
                    string json = read.Result;
                    var tags = JArray.Parse(json);
                    if (tags.Count == 0)
                    {
                        break;
                    }
                    foreach (var tag in tags)
                    {
                        if ((bool)tag["prerelease"])
                        {
                            continue;
                        }
                        latestVersion = tag["tag_name"]?.ToString();
                        if (!string.IsNullOrEmpty(latestVersion))
                        {
                            url = GetDownloadUrlFromGitHubRelease(latestVersion, owner, repo);
                            return;
                        }
                    }
                }
                else if (response.StatusCode == HttpStatusCode.Forbidden && response.ReasonPhrase.Contains("403"))
                {
                    LoggerHelper.Error("GitHub API速率限制已超出，请稍后再试。");
                    throw new Exception("GitHub API速率限制已超出，请稍后再试。");
                }
                else
                {
                    LoggerHelper.Error($"请求GitHub时发生错误: {response.StatusCode} - {response.ReasonPhrase}");
                    throw new Exception($"请求GitHub时发生错误: {response.StatusCode} - {response.ReasonPhrase}");
                }
            }
            catch (Exception e)
            {
                LoggerHelper.Error($"处理GitHub响应时发生错误: {e.Message}");
                throw new Exception($"处理GitHub响应时发生错误: {e.Message}");
            }
            page++;
        }
    }

    private static string GetDownloadUrlFromGitHubRelease(string version, string owner, string repo)
    {
        var releaseUrl = $"https://api.github.com/repos/{owner}/{repo}/releases/tags/{version}";
        using var httpClient = new HttpClient();
        httpClient.DefaultRequestHeaders.UserAgent.TryParseAdd("request");
        httpClient.DefaultRequestHeaders.Accept.TryParseAdd("application/json");
        try
        {
            var response = httpClient.GetAsync(releaseUrl).Result;
            if (response.IsSuccessStatusCode)
            {
                var read = response.Content.ReadAsStringAsync();
                read.Wait();
                var jsonResponse = read.Result;

                var releaseData = JObject.Parse(jsonResponse);

                if (releaseData["assets"] is JArray assets && assets.Count > 0)
                {
                    var targetUrl = "";
                    foreach (var asset in assets)
                    {
                        var browserDownloadUrl = asset["browser_download_url"]?.ToString();
                        if (!string.IsNullOrEmpty(browserDownloadUrl))
                        {
                            if (browserDownloadUrl.EndsWith(".zip") || browserDownloadUrl.EndsWith(".7z") || browserDownloadUrl.EndsWith(".rar"))
                            {
                                targetUrl = browserDownloadUrl;
                                break;
                            }
                        }
                    }
                    if (string.IsNullOrEmpty(targetUrl))
                    {
                        targetUrl = assets[0]["browser_downloadUrl"]?.ToString();
                    }
                    return targetUrl;
                }
            }
            else if (response.StatusCode == HttpStatusCode.Forbidden && response.ReasonPhrase.Contains("403"))
            {
                LoggerHelper.Error("GitHub API速率限制已超出，请稍后再试。");
                throw new Exception("GitHub API速率限制已超出，请稍后再试。");
            }
            else
            {
                LoggerHelper.Error($"请求GitHub时发生错误: {response.StatusCode} - {response.ReasonPhrase}");
                throw new Exception($"{response.StatusCode} - {response.ReasonPhrase}");
            }
        }
        catch (Exception e)
        {
            LoggerHelper.Error($"处理GitHub响应时发生错误: {e.Message}");
            throw new Exception($"{e.Message}");
        }
        return string.Empty;
    }

    private static void GetDownloadUrlFromMirror(string version, string resId, string cdk, out string url, out string latestVersion, string userAgent = "MFA", bool isUI = false, bool onlyCheck = false)
    {
        var cdkD = onlyCheck ? string.Empty : $"cdk={cdk}&";
        var multiplatform = MaaProcessor.Interface?.Multiplatform == true;
        var os = RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? "win" :
            RuntimeInformation.IsOSPlatform(OSPlatform.Linux) ? "linux" :
            RuntimeInformation.IsOSPlatform(OSPlatform.OSX) ? "macos" : "unknown";

        var arch = RuntimeInformation.OSArchitecture switch
        {
            Architecture.X64 => "x86_64",
            Architecture.Arm64 => "arm64",
            _ => "unknown"
        };

        var multiplatformString = multiplatform ? $"os={os}&arch={arch}&" : "";
        var releaseUrl = isUI
            ? $"https://mirrorchyan.com/api/resources/{resId}/latest?current_version={version}&{cdkD}os={os}&arch={arch}"
            : $"https://mirrorchyan.com/api/resources/{resId}/latest?current_version={version}&{cdkD}{multiplatformString}user_agent={userAgent}";
        using var httpClient = new HttpClient();
        httpClient.DefaultRequestHeaders.UserAgent.TryParseAdd("request");
        httpClient.DefaultRequestHeaders.Accept.TryParseAdd("application/json");
        try
        {
            var task = httpClient.GetAsync(releaseUrl);
            task.Wait();
            var response = task.Result;
            string message = string.Empty;

            try
            {
                response.EnsureSuccessStatusCode();
            }
            catch (HttpRequestException e)
            {
                if ((int)response.StatusCode == 403)
                {
                    message = e.Message;
                }
            }

            var read = response.Content.ReadAsStringAsync();
            read.Wait();
            var jsonResponse = read.Result;
            var responseData = JObject.Parse(jsonResponse);
            if (!string.IsNullOrWhiteSpace(message))
            {
                if (responseData["msg"] != null && responseData["msg"].ToString().ToLower().Contains("reached the most", StringComparison.OrdinalIgnoreCase))
                {
                    throw new Exception("MirrorUseLimitReached".ToLocalization());
                }
                else
                {
                    throw new Exception(message);
                }
            }
            if ((int)responseData["code"] == 0)
            {
                var data = responseData["data"];
                if (!onlyCheck && !isUI)
                    SaveAnnouncement(data, "release_note");
                var versionName = data["version_name"]?.ToString();
                var downloadUrl = data["url"]?.ToString();
                url = downloadUrl;
                latestVersion = versionName;
            }
            else
            {
                throw new Exception($"{"MirrorAutoUpdatePrompt".ToLocalization()}\n msg: {responseData["msg"]}");
            }
        }
        catch (Exception e)
        {
            throw new Exception($"{e.Message}");
        }
    }

    private static string GetLocalVersion()
    {
        return Assembly.GetExecutingAssembly().GetName().Version?.ToString() ?? "DEBUG";
    }

    private static string GetResourceVersion()
    {
        return Instances.VersionUpdateSettingsUserControlModel.ResourceVersion;
    }


    private static string GetResourceID()
    {
        return MaaProcessor.Interface?.RID ?? string.Empty;
    }

    private static bool IsNewVersionAvailable(string latestVersion, string localVersion)
    {
        try
        {
            string latestVersionNumber = ExtractVersionNumber(latestVersion);
            string localVersionNumber = ExtractVersionNumber(localVersion);

            Version latest = new Version(latestVersionNumber);
            Version local = new Version(localVersionNumber);

            return latest.CompareTo(local) > 0;
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex);
            LoggerHelper.Error(ex);
            return false;
        }
    }

    private static string ExtractVersionNumber(string versionString)
    {
        if (versionString == "Debug")
            versionString = "0.0.1";

        if (versionString.StartsWith("v") || versionString.StartsWith("V"))
        {
            versionString = versionString.Substring(1);
        }

        var parts = versionString.Split('-');
        var mainVersionPart = parts[0];

        var versionComponents = mainVersionPart.Split('.');
        while (versionComponents.Length < 4)
        {
            mainVersionPart += ".0";
            versionComponents = mainVersionPart.Split('.');
        }

        if (Version.TryParse(mainVersionPart, out _))
        {
            return mainVersionPart;
        }

        throw new FormatException("无法解析版本号: " + versionString);
    }

    async private static Task<bool> DownloadFileAsync(string url, string filePath, ProgressBar? progressBar, string key)
    {
        try
        {
            using var httpClient = new HttpClient();
            httpClient.DefaultRequestHeaders.UserAgent.ParseAdd("request");
            httpClient.DefaultRequestHeaders.Accept.ParseAdd("application/json");

            using var response = await httpClient.GetAsync(url, HttpCompletionOption.ResponseHeadersRead);
            response.EnsureSuccessStatusCode();

            var startTime = DateTime.Now;
            long totalBytesRead = 0;
            long bytesPerSecond = 0;
            long? totalBytes = response.Content.Headers.ContentLength;

            await using var contentStream = await response.Content.ReadAsStreamAsync();
            await using var fileStream = new FileStream(filePath, FileMode.Create, FileAccess.Write);

            var buffer = new byte[8192];
            var stopwatch = Stopwatch.StartNew();
            var lastSpeedUpdateTime = startTime;
            long lastTotalBytesRead = 0;

            while (true)
            {
                var bytesRead = await contentStream.ReadAsync(buffer);
                if (bytesRead == 0) break;

                await fileStream.WriteAsync(buffer.AsMemory(0, bytesRead));

                totalBytesRead += bytesRead;
                var currentTime = DateTime.Now;


                var timeSinceLastUpdate = currentTime - lastSpeedUpdateTime;
                if (timeSinceLastUpdate.TotalSeconds >= 1)
                {
                    bytesPerSecond = (long)((totalBytesRead - lastTotalBytesRead) / timeSinceLastUpdate.TotalSeconds);
                    lastTotalBytesRead = totalBytesRead;
                    lastSpeedUpdateTime = currentTime;
                }


                double progressPercentage;
                if (totalBytes.HasValue && totalBytes.Value > 0)
                {
                    progressPercentage = Math.Min((double)totalBytesRead / totalBytes.Value * 100, 100);
                }
                else
                {
                    if (bytesPerSecond > 0)
                    {
                        double estimatedTotal = totalBytesRead + bytesPerSecond * 5;
                        progressPercentage = Math.Min((double)totalBytesRead / estimatedTotal * 100, 99);
                    }
                    else
                    {
                        progressPercentage = Math.Min((currentTime - startTime).TotalSeconds / 30 * 100, 99);
                    }
                }

                SetProgress(progressBar, progressPercentage);
                if (stopwatch.ElapsedMilliseconds >= 100)
                {
                    DispatcherHelper.RunOnMainThread(() =>
                        Instances.TaskQueueViewModel.OutputDownloadProgress(
                            totalBytesRead,
                            totalBytes ?? 0,
                            (int)bytesPerSecond,
                            (currentTime - startTime).TotalSeconds));
                    stopwatch.Restart();
                }
            }

            SetProgress(progressBar, 100);
            DispatcherHelper.RunOnMainThread(() =>
                Instances.TaskQueueViewModel.OutputDownloadProgress(
                    totalBytesRead,
                    totalBytes ?? totalBytesRead,
                    (int)bytesPerSecond,
                    (DateTime.Now - startTime).TotalSeconds
                ));

            return true;
        }
        catch (HttpRequestException httpEx)
        {
            LoggerHelper.Error($"HTTP请求失败: {httpEx.Message}");
            return false;
        }
        catch (IOException ioEx)
        {
            LoggerHelper.Error($"文件操作失败: {ioEx.Message}");
            return false;
        }
        catch (Exception ex)
        {
            LoggerHelper.Error($"未知错误: {ex.Message}");
            return false;
        }
    }

    public class MirrorChangesJson
    {
        [JsonProperty("modified")] public List<string>? Modified;
        [JsonProperty("deleted")] public List<string>? Deleted;
        [JsonProperty("added")] public List<string>? Added;
        [JsonExtensionData]
        public Dictionary<string, object>? AdditionalData { get; set; } = new();
    }

    private static string[] GetRepoFromUrl(string githubUrl)
    {
        var pattern = @"^https://github\.com/(?<owner>[^/]+)/(?<repo>[^/]+)$";
        var match = Regex.Match(githubUrl, pattern);

        if (match.Success)
        {
            string owner = match.Groups["owner"].Value;
            string repo = match.Groups["repo"].Value;

            return
            [
                owner,
                repo
            ];
        }

        throw new FormatException("输入的 GitHub URL 格式不正确: " + githubUrl);
    }

    private static string CDK()
    {
        return Instances.VersionUpdateSettingsUserControlModel.CdkPassword;
    }
    private static void SetText(TextBlock? block, string text)
    {
        if (block == null)
            return;
        DispatcherHelper.RunOnMainThread(() => block.Text = text);
    }
    private static void SetProgress(ProgressBar? bar, double percentage)
    {
        if (bar == null)
            return;
        DispatcherHelper.RunOnMainThread(() => bar.Value = percentage);
    }
    private static void Dismiss(ISukiToast? toast)
    {
        if (toast == null)
            return;
        DispatcherHelper.RunOnMainThread(() => Instances.ToastManager.Dismiss(toast));
    }

    private static void CopyFolder(string sourceFolder, string destinationFolder)
    {
        if (!Directory.Exists(destinationFolder))
        {
            Directory.CreateDirectory(destinationFolder);
        }
        var files = Directory.GetFiles(sourceFolder);
        foreach (string file in files)
        {
            string fileName = Path.GetFileName(file);
            string destinationFile = Path.Combine(destinationFolder, fileName);
            File.Copy(file, destinationFile, true);
        }
        var subDirectories = Directory.GetDirectories(sourceFolder);
        foreach (string subDirectory in subDirectories)
        {
            string subDirectoryName = Path.GetFileName(subDirectory);
            string destinationSubDirectory = Path.Combine(destinationFolder, subDirectoryName);
            CopyFolder(subDirectory, destinationSubDirectory);
        }
    }

    private static void SaveAnnouncement(JToken? releaseData, string from)
    {
        try
        {
            var bodyContent = releaseData?[from]?.ToString();
            if (!string.IsNullOrWhiteSpace(bodyContent) && bodyContent != "placeholder")
            {
                var resourceDirectory = Path.Combine(AppContext.BaseDirectory, "resource");
                Directory.CreateDirectory(resourceDirectory);
                var filePath = Path.Combine(resourceDirectory, AnnouncementViewModel.AnnouncementFileName);
                File.WriteAllText(filePath, bodyContent);
                LoggerHelper.Info($"{AnnouncementViewModel.AnnouncementFileName} saved successfully.");
                GlobalConfiguration.SetValue(ConfigurationKeys.DoNotShowAgain, bool.FalseString);
            }
        }
        catch (Exception ex)
        {
            LoggerHelper.Error($"Error saving {AnnouncementViewModel.AnnouncementFileName}: {ex.Message}");
        }
    }
}
