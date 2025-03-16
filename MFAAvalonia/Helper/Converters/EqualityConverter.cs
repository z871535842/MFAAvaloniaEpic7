using Avalonia.Data.Converters;
using SukiUI.Models;
using System;
using System.Globalization;

namespace MFAAvalonia.Helper.Converters;

public class EqualityConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is SukiColorTheme currentTheme && 
            parameter is SukiColorTheme itemTheme)
        {
            return currentTheme == itemTheme;
        }
        return false;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotImplementedException();
}
