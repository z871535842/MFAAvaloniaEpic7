using System.Diagnostics;
using System.Runtime.InteropServices;

public class Program
{
    static void Main(string[] args)
    {
        Thread.Sleep(3000);
        // 验证参数数量[2,4](@ref)
        if (args.Length != 2 && args.Length != 4) // 网页[4]参数规范
        {
            Console.WriteLine("用法: MFAUpdater [源路径] [目标路径] [原程序] [新程序名称] 或者 MFAUpdater [源路径] [目标路径]");
            return;
        }


        string sourcePath = args[0];
        string destPath = args[1];

        try
        {
            if (!File.Exists(sourcePath) && !Directory.Exists(sourcePath))
            {
                Console.WriteLine($"错误：源路径 '{sourcePath}' 不存在");
                return;
            }

            // 复制操作
            if (File.Exists(sourcePath))
            {
                // 文件复制（覆盖模式）
                File.Copy(sourcePath, destPath, true);
                Console.WriteLine($"文件已复制到: {destPath}");

                // 删除源文件
                File.Delete(sourcePath);
                Console.WriteLine("源文件已删除");
            }
            else if (Directory.Exists(sourcePath))
            {
                // 目录复制（递归复制）
                Directory.CreateDirectory(destPath);
                foreach (string file in Directory.GetFiles(sourcePath))
                {
                    string fileName = Path.GetFileName(file);
                    File.Copy(file, Path.Combine(destPath, fileName), true);
                }
                // 递归处理子目录
                foreach (string dir in Directory.GetDirectories(sourcePath))
                {
                    string dirName = Path.GetFileName(dir);
                    Directory.CreateDirectory(Path.Combine(destPath, dirName));
                    // 这里可优化为递归调用
                }
                Console.WriteLine($"目录已复制到: {destPath}");

                // 删除源目录（递归删除）
                Directory.Delete(sourcePath, true);
                Console.WriteLine("源目录已删除");
            }
            
            if (args.Length == 4)
            {
                string oldAppName = args[2];
                string newAppName = args[3];
                string originalExePath = Path.Combine(AppContext.BaseDirectory, oldAppName);
                string newExePath = Path.Combine(AppContext.BaseDirectory, newAppName);

                if (File.Exists(newExePath))
                {
                    File.Delete(newExePath); // 网页[5]覆盖逻辑
                }
                File.Move(originalExePath, newExePath); // 网页[1]重命名方案
                Console.WriteLine($"程序已重命名为: {newAppName}");

                // 启动新程序
                var psi = new ProcessStartInfo
                {
                    FileName = newAppName,
                    WorkingDirectory = AppContext.BaseDirectory,
                    UseShellExecute = RuntimeInformation.IsOSPlatform(OSPlatform.Windows), // 网页[6][7]跨平台启动
                };

                using var newProcess = Process.Start(psi);
                Console.WriteLine($"已启动新进程 PID:{newProcess?.Id ?? -1}");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"操作失败: {ex.Message}");
        }
    }
}
