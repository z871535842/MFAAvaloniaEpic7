using MFAAvalonia.Helper;
using Serilog;
using System;

namespace MFAAvalonia.Helper;

public static class LoggerHelper
{
    private static readonly ILogger Logger = new LoggerConfiguration()
        .WriteTo.File(
            $"logs/log-{DateTime.Now.ToString("yyyy-MM-dd")}.txt",
            outputTemplate: "[{Timestamp:yyyy-MM-dd HH:mm:ss.fff}][{Level:u3}] {Message:lj}{NewLine}{Exception}").CreateLogger();

    public static void Info(object message)
    {
        Logger.Information(message.ToString() ?? string.Empty);
        Console.WriteLine("[INFO]" + message);
    }

    public static void Error(object message)
    {
        Logger.Error(message.ToString() ?? string.Empty);
        Console.WriteLine("[ERROR]" + message);
    }

    public static void Error(object message, Exception e)
    {
        Logger.Error(message.ToString());
        Logger.Error(e.ToString());
        Console.WriteLine("[ERROR]" + message);
        Console.WriteLine("[ERROR]" + e);
    }
    public static void Warning(object message)
    {
        Logger.Warning(message.ToString() ?? string.Empty);
        Console.WriteLine("[WARN]" + message);
    }
    
    
}
