using Avalonia.Data.Converters;
using System;
using System.Collections.Generic;
using System.Globalization;

namespace MFAAvalonia.Helper.Converters;

public class DynamicWidthConverter : IMultiValueConverter
{
    public object Convert(IList<object> values, Type targetType, object parameter, CultureInfo culture)
    {
        var minWidth = 100d;
        if (values.Count < 3 || !double.TryParse(values[0]?.ToString(), out double parentWidth) || !double.TryParse(values[1]?.ToString(), out double siblingWidth) || !double.TryParse(values[2]?.ToString(), out minWidth))
            return minWidth;

        var availableWidth = parentWidth - siblingWidth - 15;
        if (availableWidth < minWidth)
             minWidth = Math.Max(parentWidth - 15, minWidth);
        return Math.Max(availableWidth, minWidth);
    }
}
