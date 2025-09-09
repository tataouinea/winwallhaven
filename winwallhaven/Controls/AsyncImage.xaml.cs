using System;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media.Animation;
using Microsoft.UI.Xaml.Media.Imaging;

namespace winwallhaven.Controls;

public sealed partial class AsyncImage : UserControl
{
    public static readonly DependencyProperty SourceProperty = DependencyProperty.Register(
        nameof(Source), typeof(string), typeof(AsyncImage), new PropertyMetadata(default(string), OnSourceChanged));

    public AsyncImage()
    {
        InitializeComponent();
    }

    public string? Source
    {
        get => (string?)GetValue(SourceProperty);
        set => SetValue(SourceProperty, value);
    }

    private static void OnSourceChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is AsyncImage ai)
        {
            ai.Placeholder.Visibility = Visibility.Visible;
            ai.ImageElement.Opacity = 0d;
            ai.ImageElement.Source = string.IsNullOrWhiteSpace(ai.Source) ? null : new BitmapImage(new Uri(ai.Source));
        }
    }

    private void ImageElement_OnImageOpened(object sender, RoutedEventArgs e)
    {
        Placeholder.Visibility = Visibility.Collapsed;
        var fade = new DoubleAnimation
        {
            To = 1d,
            Duration = new Duration(TimeSpan.FromMilliseconds(200))
        };
        var sb = new Storyboard();
        Storyboard.SetTarget(fade, ImageElement);
        Storyboard.SetTargetProperty(fade, "Opacity");
        sb.Children.Add(fade);
        sb.Begin();
    }

    private void ImageElement_OnImageFailed(object sender, ExceptionRoutedEventArgs e)
    {
        // Show a simple error visual
        Placeholder.Visibility = Visibility.Visible;
        // Could customize error appearance here
    }
}