using MaaFramework.Binding;

namespace MFAAvalonia.Extensions.MaaFW;

public class MaaProcessor
{
    public static MaaProcessor Instance { get; } = new();
    public static MaaToolkit Toolkit { get; } = new();

    public static MaaFWConfiguration Config { get; } = new();
    
    public MaaTasker? MaaTasker { get; set; }


    public void SetTasker(MaaTasker? maaTasker = null)
    {
        MaaTasker = maaTasker;
    }

    public class MaaFWConfiguration
    {
        public AdbDeviceCoreConfig AdbDevice { get; set; } = new();
        public DesktopWindowCoreConfig DesktopWindow { get; set; } = new();
    }

    public class DesktopWindowCoreConfig
    {
        public string Name { get; set; } = string.Empty;
        public nint HWnd { get; set; }

        public Win32InputMethod Input { get; set; } = Win32InputMethod.SendMessage;

        public Win32ScreencapMethod ScreenCap { get; set; } = Win32ScreencapMethod.FramePool;
        public LinkOption Link { get; set; } = LinkOption.Start;
        public CheckStatusOption Check { get; set; } = CheckStatusOption.ThrowIfNotSucceeded;
    }

    public class AdbDeviceCoreConfig
    {
        public string Name { get; set; } = string.Empty;
        public string AdbPath { get; set; } = "adb";
        public string AdbSerial { get; set; } = "";
        public string Config { get; set; } = "{}";
        public AdbInputMethods Input { get; set; } = AdbInputMethods.Maatouch;
        public AdbScreencapMethods ScreenCap { get; set; } = AdbScreencapMethods.Default;
    }
}
