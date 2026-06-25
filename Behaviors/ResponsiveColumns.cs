using System.Windows;
using System.Windows.Controls.Primitives;

namespace stockfront.Behaviors;

/// <summary>
/// Makes a <see cref="UniformGrid"/> reflow its column count to fill the available width: as the panel
/// resizes it fits as many columns of at least <c>ItemMinWidth</c> as will go, so cards stretch evenly
/// across the row with no ragged gap on the right. Attach with
/// <c>behaviors:ResponsiveColumns.ItemMinWidth="252"</c> on the panel inside the ItemsPanelTemplate; the
/// items themselves should stretch (no fixed Width) so each fills its equal-width cell.
/// </summary>
public static class ResponsiveColumns
{
    public static readonly DependencyProperty ItemMinWidthProperty =
        DependencyProperty.RegisterAttached(
            "ItemMinWidth", typeof(double), typeof(ResponsiveColumns),
            new PropertyMetadata(0d, OnItemMinWidthChanged));

    public static void SetItemMinWidth(DependencyObject element, double value) =>
        element.SetValue(ItemMinWidthProperty, value);

    public static double GetItemMinWidth(DependencyObject element) =>
        (double)element.GetValue(ItemMinWidthProperty);

    private static void OnItemMinWidthChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is not UniformGrid grid)
            return;

        grid.SizeChanged -= OnSizeChanged;
        if ((double)e.NewValue > 0)
        {
            grid.SizeChanged += OnSizeChanged;
            UpdateColumns(grid);
        }
    }

    private static void OnSizeChanged(object sender, SizeChangedEventArgs e)
    {
        if (e.WidthChanged && sender is UniformGrid grid)
            UpdateColumns(grid);
    }

    private static void UpdateColumns(UniformGrid grid)
    {
        var min = GetItemMinWidth(grid);
        if (min <= 0 || grid.ActualWidth <= 0)
            return;

        grid.Columns = Math.Max(1, (int)(grid.ActualWidth / min));
    }
}
