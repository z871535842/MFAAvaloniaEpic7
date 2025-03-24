using CommunityToolkit.Mvvm.ComponentModel;
using MFAAvalonia.Extensions.MaaFW;
using MFAAvalonia.Views.Windows;
using MFAAvalonia.Helper;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace MFAAvalonia.Helper.ValueType;

public partial class MFATask : ObservableObject
{
    public enum MFATaskType
    {
        MFA,
        MAAFW
    }

    [ObservableProperty] private string? _name = string.Empty;
    [ObservableProperty] private MFATaskType _type = MFATaskType.MFA;
    [ObservableProperty] private int _count = 1;
    [ObservableProperty] private Func<Task> _action;
    [ObservableProperty] private Dictionary<string, MaaNode> _tasks = new();


    public async Task<bool> Run(CancellationToken token)
    {
        try
        {
            for (int i = 0; i < Count; i++)
            {
                token.ThrowIfCancellationRequested();
                if (Type == MFATaskType.MAAFW)
                    RootView.AddLogByKey("TaskStart", null,true, Name ?? string.Empty);
                await Action();
            }
            return true;
        }
        catch (OperationCanceledException)
        {
            return false;
        }
        catch (Exception ex)
        {
            LoggerHelper.Error(ex);
            return false;
        }
    }
}
