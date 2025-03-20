using Avalonia.Markup.Xaml.MarkupExtensions;
using AvaloniaExtensions.Axaml.Markup;
using MFAAvalonia.Helper;
using MFAAvalonia.ViewModels.Other;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text.RegularExpressions;

namespace MFAAvalonia.Extensions;

public static class MFAExtensions
{
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
}
