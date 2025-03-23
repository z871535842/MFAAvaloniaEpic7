using MFAAvalonia.Extensions.MaaFW;
using MFAWPF.Helper;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace MFAAvalonia.Helper;

public static class EmulatorHelper
{
    private static readonly IReadOnlyDictionary<string, EmulatorProfile> EmulatorProfiles = new Dictionary<string, EmulatorProfile>
    {
        ["MuMuPlayer12"] = new EmulatorProfile(
            Processes: new[]
            {
                "MuMuPlayer"
            },
            ConsoleCli: "MuMuManager",
            ArgsPattern: "api -v {0} shutdown_player",
            PortRule: address => address == "127.0.0.1:16384" ? 0 : (int.Parse(address.Split(':')[1]) - 16384) / 32
        ),
        ["LDPlayer"] = new EmulatorProfile(
            Processes: new[]
            {
                "dnplayer",
                "ldplayer"
            },
            ConsoleCli: "ldconsole",
            ArgsPattern: "quit --index {0}",
            PortRule: address => address.Contains(':')
                ? (int.Parse(address.Split(':')[1]) - 5555) / 2
                : (int.Parse(address.Split('-')[1]) - 5554) / 2
        ),
        ["Nox"] = new EmulatorProfile(
            Processes: new[]
            {
                "Nox",
                "NoxVM"
            },
            ConsoleCli: "NoxConsole",
            ArgsPattern: "quit -index:{0}",
            PortRule: address => address == "127.0.0.1:62001" ? 0 : int.Parse(address.Split(':')[1]) - 62024
        ),
        ["XYAZ"] = new EmulatorProfile(
            Processes: new[]
            {
                "MEmu"
            },
            ConsoleCli: "memuc",
            ArgsPattern: "stop -i {0}",
            PortRule: address => (int.Parse(address.Split(':')[1]) - 21503) / 10
        ),
        ["BlueStacks"] = new EmulatorProfile(
            Processes: new[]
            {
                "HD-Player",
                "BlueStacks"
            },
            ConsoleCli: "bsconsole",
            ArgsPattern: "shutdown",
            PortRule: _ => 0
        )
    };

    public static async Task<bool> KillEmulatorAsync()
    {
        try
        {
            var address = MaaProcessor.Config.AdbDevice.AdbSerial;
            var emulatorType = DetectEmulatorType(address);

            // 优先尝试ADB命令关闭
            if (await ExecuteAdbCommandAsync($"-s {address} emu kill"))
                return true;

            // 根据模拟器类型使用专用方法
            if (EmulatorProfiles.TryGetValue(emulatorType, out var profile))
            {
                return await KillByProfileAsync(address, profile);
            }

            // 通用关闭流程
            return await KillGenericEmulatorAsync(address);
        }
        catch (Exception e)
        {
            LoggerHelper.Error($"Emulator kill failed: {e.Message}");
            return false;
        }
    }
    private static async Task<bool> KillGenericEmulatorAsync(string address)
    {
        // 尝试终止所有已知模拟器进程
        foreach (var profile in EmulatorProfiles.Values)
        {
            if (KillProcesses(profile.Processes))
                return true;
        }

        // 尝试通过端口终止
        var port = ParsePortFromAddress(address);
        if (port > 0 && await KillByPortAsync(port))
            return true;

        // 最后尝试强制ADB终止
        return await ExecuteAdbCommandAsync($"-s {address} kill-server");
    }

    private static string DetectEmulatorType(string address)
    {
        foreach (var kv in EmulatorProfiles)
        {
            if (address.Contains(kv.Key, StringComparison.OrdinalIgnoreCase))
                return kv.Key;
        }
        return "Unknown";
    }

    private static async Task<bool> KillByProfileAsync(string address, EmulatorProfile profile)
    {
        // 尝试通过控制台关闭
        if (await TryKillViaConsoleCliAsync(address, profile))
            return true;

        // 终止相关进程
        if (KillProcesses(profile.Processes))
            return true;

        // 端口检测终止
        return await KillByPortAsync(ParsePortFromAddress(address));
    }

    private static async Task<bool> TryKillViaConsoleCliAsync(string address, EmulatorProfile profile)
    {
        try
        {
            var index = profile.PortRule(address);
            var cliPath = FindEmulatorConsole(profile.ConsoleCli);

            if (string.IsNullOrEmpty(cliPath))
                return false;

            var arguments = string.Format(profile.ArgsPattern, index);
            var (exitCode, _) = await ExecuteShellCommandAsync(cliPath, arguments);
            return exitCode == 0;
        }
        catch
        {
            return false;
        }
    }


    private static string FindEmulatorConsole(string cliName)
    {
        var searchPaths = new List<string>();

        // Windows路径
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            searchPaths.AddRange(new[]
            {
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), cliName),
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86), cliName)
            });
        }
        // Linux/macOS路径
        else
        {
            searchPaths.AddRange(new[]
            {
                $"/usr/local/bin/{cliName}",
                $"/opt/{cliName}/bin/{cliName}",
                $"/Applications/{cliName}.app/Contents/MacOS/{cliName}"
            });
        }

        foreach (var path in searchPaths)
        {
            if (File.Exists(path))
                return path;

            var withExt = Path.ChangeExtension(path, RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? "exe" : null);
            if (File.Exists(withExt))
                return withExt;
        }

        return null;
    }

    private static bool KillProcesses(IEnumerable<string> processNames)
    {
        foreach (var name in processNames)
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                if (ExecuteShellCommand("taskkill", $"/F /IM {name}.exe"))
                    return true;
            }
            else
            {
                if (ExecuteShellCommand("pkill", $"-9 {name}") || ExecuteShellCommand("killall", $"-9 {name}"))
                    return true;
            }
        }
        return false;
    }

    private static async Task<bool> KillByPortAsync(int port)
    {
        var (pid, _) = await GetProcessIdByPortAsync(port);
        if (pid <= 0) return false;

        try
        {
            var process = Process.GetProcessById(pid);
            process.Kill();
            return process.WaitForExit(5000);
        }
        catch
        {
            return false;
        }
    }

    private static async Task<(int Pid, string Output)> GetProcessIdByPortAsync(int port)
    {
        try
        {
            string command;
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                command = $"netstat -ano | findstr :{port}";
            }
            else
            {
                command = $"lsof -i :{port} | awk '{{print $2}}' | tail -n 1";
            }

            var result = await ExecuteShellCommandAsync("sh", $"-c \"{command}\"");
            return (int.Parse(result.Output), result.Output);
        }
        catch
        {
            return (-1, string.Empty);
        }
    }

    private static async Task<bool> ExecuteAdbCommandAsync(string arguments)
    {
        var adbPath = FindAdbPath() ?? "adb";
        var result = await ExecuteShellCommandAsync(adbPath, arguments);
        return result.ExitCode == 0;
    }

    private static string FindAdbPath()
    {
        var paths = new List<string>();
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            paths.AddRange(new[]
            {
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "Android\\android-sdk\\platform-tools\\adb.exe"),
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "AppData\\Local\\Android\\Sdk\\platform-tools\\adb.exe")
            });
        }
        else
        {
            paths.AddRange(new[]
            {
                "/usr/bin/adb",
                "/usr/local/bin/adb",
                "/opt/android-sdk/platform-tools/adb"
            });
        }

        foreach (var path in paths)
        {
            if (File.Exists(path))
                return path;
        }
        return null;
    }

    async private static Task<(int ExitCode, string Output)> ExecuteShellCommandAsync(string fileName, string arguments)
    {
        try
        {
            var startInfo = new ProcessStartInfo
            {
                FileName = fileName,
                Arguments = arguments,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            using var process = new Process
            {
                StartInfo = startInfo
            };
            process.Start();
            var output = await process.StandardOutput.ReadToEndAsync();
            await process.WaitForExitAsync();
            return (process.ExitCode, output);
        }
        catch (Exception e)
        {
            LoggerHelper.Error($"Command execution failed: {e.Message}");
            return (-1, string.Empty);
        }
    }

    private static bool ExecuteShellCommand(string fileName, string arguments)
    {
        try
        {
            var startInfo = new ProcessStartInfo
            {
                FileName = fileName,
                Arguments = arguments,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            using var process = Process.Start(startInfo);
            return process != null && process.WaitForExit(5000) && process.ExitCode == 0;
        }
        catch
        {
            return false;
        }
    }

    private static int ParsePortFromAddress(string address)
    {
        var match = Regex.Match(address, @":(\d+)");
        return match.Success ? int.Parse(match.Groups[1].Value) : 0;
    }

    private record EmulatorProfile(
        string[] Processes,
        string ConsoleCli,
        string ArgsPattern,
        Func<string, int> PortRule
    );
}
