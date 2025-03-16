using Avalonia.Data;
using Avalonia.Data.Converters;
using MFAAvalonia.Extensions.MaaFW;
using System;
using System.Globalization;

namespace MFAAvalonia.Helper.Converters;

public class EnumToBoolConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value == null || parameter == null) return false;
        return value.Equals(parameter);
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is true && parameter is MaaControllerTypes type)
            return type;
        return BindingOperations.DoNothing;
    }
}