using Microsoft.UI;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Media;
using System;
using System.Globalization;
using Windows.UI;

namespace winwallhaven.Converters;

public sealed class HexColorToBrushConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language)
    {
        var hex = value as string;
        if (string.IsNullOrWhiteSpace(hex)) return new SolidColorBrush(Colors.Transparent);
        if (hex.StartsWith('#')) hex = hex[1..];
        if (hex.Length is 3) hex = string.Concat(hex[0], hex[0], hex[1], hex[1], hex[2], hex[2]);
        if (hex.Length != 6) return new SolidColorBrush(Colors.Transparent);
        try
        {
            var r = byte.Parse(hex.Substring(0, 2), NumberStyles.HexNumber);
            var g = byte.Parse(hex.Substring(2, 2), NumberStyles.HexNumber);
            var b = byte.Parse(hex.Substring(4, 2), NumberStyles.HexNumber);
            return new SolidColorBrush(Color.FromArgb(255, r, g, b));
        }
        catch
        {
            return new SolidColorBrush(Colors.Transparent);
        }
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language)
    {
        throw new NotSupportedException();
    }
}