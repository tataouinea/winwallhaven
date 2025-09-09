using System;
using Microsoft.UI.Xaml.Data;

namespace winwallhaven.Converters;

public sealed class FileSizeToStringConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language)
    {
        if (value is not long l || l <= 0) return "--";
        string[] units = { "B", "KB", "MB", "GB", "TB" };
        double size = l;
        var unit = 0;
        while (size >= 1024 && unit < units.Length - 1)
        {
            size /= 1024;
            unit++;
        }

        return $"{size:0.##} {units[unit]}";
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language)
    {
        throw new NotSupportedException();
    }
}