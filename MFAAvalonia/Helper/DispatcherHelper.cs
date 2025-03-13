using Avalonia;
using Avalonia.Threading;
using System;

namespace MFAAvalonia.Helper;

public static class DispatcherHelper
{
    public static void RunOnMainThread(Action action)
    {
        if (Dispatcher.UIThread.CheckAccess())
        {
            action();
        }
        else
        {
            Dispatcher.UIThread.Post(action);
        }
    }
}
