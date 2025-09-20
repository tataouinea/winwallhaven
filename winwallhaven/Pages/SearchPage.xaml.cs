using System;
using Windows.System;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media.Imaging;
using winwallhaven.Core.Models;
using winwallhaven.Services;
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

    private void MinResolutionFlyout_Opening(object sender, object e)
    {
        if (DataContext is not SearchViewModel vm) return;
        if (vm.Filters.MinWidth == null || vm.Filters.MinHeight == null)
            if (ScreenResolutionHelper.TryGetCurrentMonitorResolution(out var w, out var h))
            {
                vm.Filters.MinWidth = w;
                vm.Filters.MinHeight = h;
            }
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

    private void ResetImageZoom()
    {
        // Reset zoom factor to 1.0 to ensure proper fitting
        DispatcherQueue.TryEnqueue(() =>
        {
            try
            {
                ImageScrollViewer?.ZoomToFactor(1.0f);
            }
            catch
            {
                // Ignore any exceptions during zoom reset
            }
        });
    }

    private void FullScreenImage_ImageOpened(object sender, RoutedEventArgs e)
    {
        // When image loads, fit it to the available space
        FitImageToViewport();
    }

    private void ImageScrollViewer_SizeChanged(object sender, SizeChangedEventArgs e)
    {
        // When the container size changes, refit the image
        if (FullScreenOverlay.Visibility == Visibility.Visible) FitImageToViewport();
    }

    private void FitImageToViewport()
    {
        DispatcherQueue.TryEnqueue(() =>
        {
            try
            {
                var scrollViewer = ImageScrollViewer;
                var image = FullScreenImage;

                if (scrollViewer != null && image?.Source != null)
                {
                    // First, reset zoom to calculate natural sizes
                    scrollViewer.ZoomToFactor(1.0f);

                    // Give the UI time to update, then calculate fit zoom
                    DispatcherQueue.TryEnqueue(() =>
                    {
                        var viewportWidth = scrollViewer.ViewportWidth;
                        var viewportHeight = scrollViewer.ViewportHeight;
                        var contentWidth = scrollViewer.ExtentWidth;
                        var contentHeight = scrollViewer.ExtentHeight;

                        if (viewportWidth > 0 && viewportHeight > 0 && contentWidth > 0 && contentHeight > 0)
                        {
                            var scaleX = viewportWidth / contentWidth;
                            var scaleY = viewportHeight / contentHeight;
                            var scale = Math.Min(scaleX, scaleY);

                            // Only scale down if the image is larger than the viewport
                            if (scale < 1.0) scrollViewer.ZoomToFactor((float)scale);
                        }
                    });
                }
            }
            catch
            {
                // Ignore any exceptions
            }
        });
    }

    private void UseCurrentScreen_Click(object sender, RoutedEventArgs e)
    {
        if (DataContext is not SearchViewModel vm) return;
        if (ScreenResolutionHelper.TryGetCurrentMonitorResolution(out var w, out var h))
        {
            vm.Filters.MinWidth = w;
            vm.Filters.MinHeight = h;
            vm.ApplyFiltersCommand?.Execute(null);
        }
    }

    private void ClearMinResolution_Click(object sender, RoutedEventArgs e)
    {
        if (DataContext is not SearchViewModel vm) return;
        vm.Filters.ClearMinResolution();
        vm.ApplyFiltersCommand?.Execute(null);
    }

    private void PresetResolution_Click(object sender, RoutedEventArgs e)
    {
        if (DataContext is not SearchViewModel vm) return;
        if (sender is Button b && b.Tag is string tag && TryParseRes(tag, out var w, out var h))
        {
            vm.Filters.MinWidth = w;
            vm.Filters.MinHeight = h;
            vm.ApplyFiltersCommand?.Execute(null);
        }
    }

    private void ApplyMinResolution_Click(object sender, RoutedEventArgs e)
    {
        if (DataContext is not SearchViewModel vm) return;
        // Pull values from NumberBoxes (double -> int)
        try
        {
            var w = (int)Math.Max(0, MinWidthBox.Value);
            var h = (int)Math.Max(0, MinHeightBox.Value);
            vm.Filters.MinWidth = w > 0 ? w : null;
            vm.Filters.MinHeight = h > 0 ? h : null;
        }
        catch
        {
            // ignore invalid inputs
        }

        vm.ApplyFiltersCommand?.Execute(null);
    }

    private static bool TryParseRes(string s, out int w, out int h)
    {
        w = h = 0;
        var parts = s.Split('x', '×');
        if (parts.Length != 2) return false;
        return int.TryParse(parts[0], out w) && int.TryParse(parts[1], out h);
    }
}