using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace winwallhaven.Responsive;

/// <summary>
///     Helper that dynamically sets ItemWidth on an ItemsWrapGrid so that:
///     1. All rows are filled (column count divides the configured page size, e.g. 96)
///     2. Available width is used (respecting min/max item width constraints)
/// </summary>
public static class GridViewColumnSizer
{
    public static readonly DependencyProperty EnableResponsiveColumnsProperty = DependencyProperty.RegisterAttached(
        "EnableResponsiveColumns", typeof(bool), typeof(GridViewColumnSizer),
        new PropertyMetadata(false, OnEnableChanged));

    public static readonly DependencyProperty PageSizeProperty = DependencyProperty.RegisterAttached(
        "PageSize", typeof(int), typeof(GridViewColumnSizer), new PropertyMetadata(50, OnLayoutPropertyChanged));

    public static readonly DependencyProperty MinItemWidthProperty = DependencyProperty.RegisterAttached(
        "MinItemWidth", typeof(double), typeof(GridViewColumnSizer),
        new PropertyMetadata(160d, OnLayoutPropertyChanged));

    public static readonly DependencyProperty MaxItemWidthProperty = DependencyProperty.RegisterAttached(
        "MaxItemWidth", typeof(double), typeof(GridViewColumnSizer),
        new PropertyMetadata(320d, OnLayoutPropertyChanged));

    public static void SetEnableResponsiveColumns(DependencyObject element, bool value)
    {
        element.SetValue(EnableResponsiveColumnsProperty, value);
    }

    public static bool GetEnableResponsiveColumns(DependencyObject element)
    {
        return (bool)element.GetValue(EnableResponsiveColumnsProperty);
    }

    public static void SetPageSize(DependencyObject element, int value)
    {
        element.SetValue(PageSizeProperty, value);
    }

    public static int GetPageSize(DependencyObject element)
    {
        return (int)element.GetValue(PageSizeProperty);
    }

    public static void SetMinItemWidth(DependencyObject element, double value)
    {
        element.SetValue(MinItemWidthProperty, value);
    }

    public static double GetMinItemWidth(DependencyObject element)
    {
        return (double)element.GetValue(MinItemWidthProperty);
    }

    public static void SetMaxItemWidth(DependencyObject element, double value)
    {
        element.SetValue(MaxItemWidthProperty, value);
    }

    public static double GetMaxItemWidth(DependencyObject element)
    {
        return (double)element.GetValue(MaxItemWidthProperty);
    }

    private static void OnEnableChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is GridView gv)
        {
            if ((bool)e.NewValue)
            {
                gv.Loaded += GridViewOnLoaded;
                gv.SizeChanged += GridViewOnSizeChanged;
                Apply(gv);
            }
            else
            {
                gv.Loaded -= GridViewOnLoaded;
                gv.SizeChanged -= GridViewOnSizeChanged;
            }
        }
    }

    private static void GridViewOnLoaded(object sender, RoutedEventArgs e)
    {
        if (sender is GridView gv) Apply(gv);
    }

    private static void OnLayoutPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is GridView gv && GetEnableResponsiveColumns(gv)) Apply(gv);
    }

    private static void GridViewOnSizeChanged(object sender, SizeChangedEventArgs e)
    {
        if (sender is GridView gv) Apply(gv);
    }

    private static void Apply(GridView gv)
    {
        if (gv.ItemsPanelRoot is not ItemsWrapGrid wrapGrid) return;
        var width = gv.ActualWidth;
        if (width <= 0) return;

        // Exclude padding (gv.Padding) and a small scrollbar allowance
        var horizontalPadding = gv.Padding.Left + gv.Padding.Right + 8; // scrollbar allowance
        var available = width - horizontalPadding;
        if (available <= 0) return;

        var pageSize = GetPageSize(gv);
        var minW = GetMinItemWidth(gv);
        var maxW = GetMaxItemWidth(gv);

        // Consider the item margin from ItemContainerStyle (Margin="10") -> horizontal margin per item = 20.
        const double itemHorizontalMargin = 20;
        var bestItemWidth = minW;
        for (var cols = 1; cols <= pageSize; cols++)
        {
            if (pageSize % cols != 0) continue; // must divide page size for full rows
            var candidate = (available - cols * itemHorizontalMargin) / cols;
            if (candidate < minW) break; // adding more columns only shrinks width further
            if (candidate > maxW) candidate = maxW;
            bestItemWidth = candidate; // prefer more columns while valid
        }

        wrapGrid.ItemWidth = bestItemWidth;
        wrapGrid.ItemHeight = bestItemWidth * 0.5625 + 60; // 16:9 image + metadata panel
    }
}