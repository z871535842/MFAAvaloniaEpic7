using System.Diagnostics;
using System.Runtime.InteropServices;
using System.IO;
using System.Threading;

public class Program
{
    private const int InitDelay = 2500;
    static void Main(string[] args)
    {
        Thread.Sleep(InitDelay); // 网页[6]启动延迟优化

        ValidateArguments(args); // 参数验证模块化
        HandleFileOperations(args); // 主操作封装
    }

    private static void ValidateArguments(string[] args)
    {
        if (args.Length is not (2 or 4)) // C# 9模式匹配
        {
            Console.WriteLine("""
                              用法: 
                              MFAUpdater [源路径] [目标路径] [原程序] [新程序名称] 
                              或 
                              MFAUpdater [源路径] [目标路径]
                              """);
            Environment.Exit(1); // 标准化退出码
        }
    }

    private static void HandleFileOperations(string[] args)
    {
        try
        {
            var (source, dest) = (args[0], args[1]);

            if (File.Exists(source))
                HandleFileTransfer(source, dest);
            else if (Directory.Exists(source))
                HandleDirectoryTransfer(source, dest);
            else
                throw new FileNotFoundException($"路径不存在: {source}");
            
            if (args.Length == 4)
                HandleAppRename(source, dest, args[2], args[3]); // 重命名与启动分离
            
            HandleDeleteDirectoryTransfer(source);
        }
        catch (Exception ex)
        {
            HandlePlatformSpecificErrors(ex); // 跨平台错误处理
            Environment.Exit(ex.HResult); // 返回错误码
        }
    }

    private static void HandleFileTransfer(string source, string dest)
    {
        try
        {
            if (dest.Contains("MFAAvalonia.dll", StringComparison.OrdinalIgnoreCase) || !dest.Contains("MFAUpdater", StringComparison.OrdinalIgnoreCase) && !dest.Contains("MFAAvalonia", StringComparison.OrdinalIgnoreCase))
            {
                File.Copy(source, dest, true);
                Console.WriteLine($"From {source} to {dest}");
            }
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
        }

        SetUnixPermissions(dest); // 网页[6][7]权限设置
    }

    private static void HandleDirectoryTransfer(string source, string dest)
    {
        Directory.CreateDirectory(dest);

        // 递归复制优化（网页[1]目录处理）
        foreach (var dir in Directory.GetDirectories(source, "*", SearchOption.AllDirectories))
            Directory.CreateDirectory(dir.Replace(source, dest));

        foreach (var file in Directory.GetFiles(source, "*", SearchOption.AllDirectories))
        {
            var destFile = file.Replace(source, dest);
            HandleFileTransfer(file, destFile);
        }

        Console.WriteLine($"目录迁移完成: {dest}");
    }

    private static void HandleDeleteDirectoryTransfer(string source)
    {
        try
        {
            Directory.Delete(source, true);
            Console.WriteLine($"源目录{source}删除完成");
        }
        catch (Exception e)
        {
            Console.WriteLine($"源目录{source}删除失败: {e}");
        }

    }

    private static void HandleAppRename(string source, string dest, string oldName, string newName)
    {
        var oldPath = Path.Combine(source, oldName);
        var newPath = Path.Combine(dest, newName);

        File.Move(oldPath, newPath, true);

        SetUnixPermissions(newPath); // 网页[6]可执行权限

        StartCrossPlatformProcess(newName); // 统一启动入口
    }

    private static void SetUnixPermissions(string path)
    {
        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows)) // 网页[1][7]平台检测
        {
            try
            {
                using var chmod = Process.Start(new ProcessStartInfo
                {
                    FileName = "chmod",
                    Arguments = $"+rwx \"{path}\"",
                    UseShellExecute = false
                });
                chmod?.WaitForExit();
            }
            catch
            {
                Console.WriteLine($"权限设置失败，请手动执行: sudo chmod 755 \"{path}\"");
            }
        }
    }

    private static void StartCrossPlatformProcess(string appName)
    {
        try
        {
            var startInfo = new ProcessStartInfo
            {
                WorkingDirectory = AppContext.BaseDirectory,
                UseShellExecute = RuntimeInformation.IsOSPlatform(OSPlatform.Windows),
                FileName = RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? appName : $"./{appName}"
            };

            using var process = Process.Start(startInfo);
            Console.WriteLine($"进程已启动 [PID:{process?.Id}]");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"启动失败: {ex.Message}");
            if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                Console.WriteLine($"尝试: chmod +x {appName} && ./{appName}");
        }
    }


    private static void HandlePlatformSpecificErrors(Exception ex)
    {
        Console.WriteLine($"错误: {ex.Message}");

        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows) && ex is UnauthorizedAccessException)
        {
            Console.WriteLine("""
                              Linux/macOS 权限问题解决方案:
                              1. 使用 sudo 重新运行
                              2. 检查文件所有权: ls -l
                              3. 手动设置权限: chmod -R 755 [目标目录]
                              """);
        }
    }
}
