using System;
using Windows.UI;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Media;

namespace winwallhaven.Converters;

public sealed class PurityToBrushConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language)
    {
        var purity = (value as string ?? string.Empty).ToLowerInvariant();
        // Choose colors: sfw (green), sketchy (orange), nsfw (red)
        var color = purity switch
        {
            "sfw" => Color.FromArgb(180, 34, 139, 34),
            "sketchy" => Color.FromArgb(180, 255, 140, 0),
            "nsfw" => Color.FromArgb(200, 178, 34, 34),
            _ => Color.FromArgb(160, 96, 96, 96)
        };
        return new SolidColorBrush(color);
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language)
    {
        throw new NotSupportedException();
    }
}