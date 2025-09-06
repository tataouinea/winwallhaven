using System;
using Windows.System;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media.Imaging;
using winwallhaven.Core.Models;
using winwallhaven.ViewModels;

namespace winwallhaven.Pages;

public sealed partial class SearchPage : Page
{
    public SearchPage()
    {
        InitializeComponent();
        // Resolve VM from DI
        DataContext = App.Services.GetRequiredService<SearchViewModel>();

        // Handle keyboard shortcuts for full-screen preview
        KeyDown += SearchPage_KeyDown;
    }

    private void SearchPage_KeyDown(object sender, KeyRoutedEventArgs e)
    {
        // Close preview with Escape key
        if (e.Key == VirtualKey.Escape && FullScreenOverlay.Visibility == Visibility.Visible)
        {
            CloseFullScreenPreview();
            e.Handled = true;
        }
    }

    private void Image_DoubleTapped(object sender, DoubleTappedRoutedEventArgs e)
    {
        if (sender is Image image && image.Tag is Wallpaper wallpaper) ShowFullScreenPreview(wallpaper);
    }

    private void ClosePreviewButton_Click(object sender, RoutedEventArgs e)
    {
        CloseFullScreenPreview();
    }

    private void ShowFullScreenPreview(Wallpaper wallpaper)
    {
        // Set the full-resolution image source
        FullScreenImage.Source = new BitmapImage(new Uri(wallpaper.Path));

        // Update info panel
        ImageIdText.Text = $"ID: {wallpaper.Id}";
        ImageDimensionsText.Text = $"{wallpaper.Width} × {wallpaper.Height}";

        // Show the overlay
        FullScreenOverlay.Visibility = Visibility.Visible;

        // Focus the close button for keyboard navigation
        ClosePreviewButton.Focus(FocusState.Programmatic);
    }

    private void CloseFullScreenPreview()
    {
        FullScreenOverlay.Visibility = Visibility.Collapsed;

        // Clear the image source to free memory
        FullScreenImage.Source = null;
    }
}