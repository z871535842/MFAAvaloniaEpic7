using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;

namespace MFAAvalonia.Helper;

public class AvaloniaMemoryCracker
{
    #region 平台相关API

    [DllImport("kernel32.dll")]
    private extern static bool SetProcessWorkingSetSize(IntPtr proc, int min, int max);

    private static bool IsWindows =>
        RuntimeInformation.IsOSPlatform(OSPlatform.Windows);

    #endregion

    #region 核心逻辑

    /// <summary>启动内存优化守护进程</summary>
    /// <param name="intervalSeconds">清理间隔秒数（默认30秒）</param>
    public void Cracker(int intervalSeconds = 30)
    {
        Task.Factory.StartNew(() =>
        {
            while (true)
            {
                try
                {
                    PerformMemoryCleanup();
                    Thread.Sleep(TimeSpan.FromSeconds(intervalSeconds));
                }
                catch
                {
                    // 异常处理可扩展日志记录
                }
            }
        }, TaskCreationOptions.LongRunning);
    }

    /// <summary>执行内存清理三步策略</summary>
    private void PerformMemoryCleanup()
    {
        // 第一步：触发托管堆GC回收[1](@ref)
        GC.Collect(GC.MaxGeneration, GCCollectionMode.Forced);
        GC.WaitForPendingFinalizers();

        // 第二步：针对Windows平台优化工作集[1](@ref)
        if (IsWindows)
        {
            SetProcessWorkingSetSize(
                Process.GetCurrentProcess().Handle,
                -1, -1
            );
        }

        // 第三步：可选扩展点（如内存池管理）
        // 可在此处集成引用计数或内存碎片整理逻辑[1](@ref)
    }

    #endregion
}
