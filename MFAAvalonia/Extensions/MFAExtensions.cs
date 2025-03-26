using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Notifications;
using Avalonia.Markup.Xaml.MarkupExtensions;
using Avalonia.Threading;
using AvaloniaExtensions.Axaml.Markup;
using MFAAvalonia.Extensions.MaaFW;
using MFAAvalonia.Helper;
using MFAAvalonia.ViewModels.Other;
using Microsoft.Extensions.DependencyInjection;
using SukiUI.Controls;
using SukiUI.Dialogs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace MFAAvalonia.Extensions;

public static class MFAExtensions
{
    public static Dictionary<TKey, MaaNode> MergeMaaNodes<TKey>(
        this IEnumerable<KeyValuePair<TKey, MaaNode>>? taskModels,
        IEnumerable<KeyValuePair<TKey, MaaNode>>? additionalModels) where TKey : notnull
    {
        
        if (additionalModels == null)
            return taskModels?.ToDictionary() ?? new Dictionary<TKey, MaaNode>();
        return taskModels?
                .Concat(additionalModels)
                .GroupBy(pair => pair.Key)
                .ToDictionary(
                    group => group.Key,
                    group =>
                    {
                        var mergedModel = group.First().Value;
                        foreach (var taskModel in group.Skip(1))
                        {
                            mergedModel.Merge(taskModel.Value);
                        }

                        return mergedModel;
                    }
                )
            ?? new Dictionary<TKey, MaaNode>();
    }
    
    public static string FormatWith(this string format, params object[] args)
    {
        return string.Format(format, args);
    }

    public static void AddRange<T>(this ICollection<T> collection, IEnumerable<T> newItems)
    {
        if (collection == null)
            return;
        if (collection is List<T> objList)
        {
            objList.AddRange(newItems);
        }
        else
        {
            foreach (T newItem in newItems)
                collection.Add(newItem);
        }
    }

    public static string ToLocalization(this string? key)
    {
        if (string.IsNullOrWhiteSpace(key))
            return string.Empty;

        return I18nManager.GetString(key) ?? key;
    }

    public static string ToLocalizationFormatted(this string? key, bool transformKey = true, params string[] args)
    {
        if (string.IsNullOrWhiteSpace(key)) return string.Empty;

        var localizedKey = key.ToLocalization();
        var processedArgs = Array.ConvertAll(args, a => a.ToLocalization() as object);

        try
        {
            return Regex.Unescape(localizedKey.FormatWith(processedArgs));
        }
        catch
        {
            return localizedKey.FormatWith(processedArgs);
        }
    }
    
    public static bool ContainsKey(this IEnumerable<LocalizationViewModel> settingViewModels, string key)
    {
        return settingViewModels.Any(vm => vm.ResourceKey == key);
    }
    
    public static bool ShouldSwitchButton(this List<MaaInterface.MaaInterfaceOptionCase>? cases, out int yes, out int no)
    {
        yes = -1;
        no = -1;

        if (cases == null || cases.Count != 2)
            return false;

        var yesItem = cases
            .Select((c, index) => new
            {
                c.Name,
                Index = index
            })
            .Where(x => x.Name?.Equals("yes", StringComparison.OrdinalIgnoreCase) == true).ToList();

        var noItem = cases
            .Select((c, index) => new
            {
                c.Name,
                Index = index
            })
            .Where(x => x.Name?.Equals("no", StringComparison.OrdinalIgnoreCase) == true).ToList();

        if (yesItem.Count == 0 || noItem.Count == 0)
            return false;

        yes = yesItem[0].Index;
        no = noItem[0].Index;

        return true;
    }
    
    public static void SafeCancel(this CancellationTokenSource? cts, bool useCancel = true)
    {
        if (cts == null || cts.IsCancellationRequested) return;

        try
        {
            if (useCancel) cts.Cancel();
            cts.Dispose();
        }
        catch (Exception e) { Console.WriteLine(e); }
    }
}
