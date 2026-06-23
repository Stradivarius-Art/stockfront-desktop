using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using StockFront.Contracts.Warehouse;

namespace stockfront.Behaviors;

/// <summary>
/// Adds a type-to-filter search box to the top of a ComboBox drop-down, so a long product list can be
/// narrowed instead of scrolled. Attach with <c>behaviors:ComboBoxSearch.Enabled="True"</c>; the
/// ComboBox must use a template whose popup contains a <see cref="TextBox"/> named
/// <c>PART_SearchBox</c> (see the <c>SearchableCombo</c> style). Filtering is by product Name/SKU and is
/// cleared whenever the drop-down closes, so the bound source stays whole for everyone else.
/// </summary>
public static class ComboBoxSearch
{
    public static readonly DependencyProperty EnabledProperty =
        DependencyProperty.RegisterAttached(
            "Enabled", typeof(bool), typeof(ComboBoxSearch),
            new PropertyMetadata(false, OnEnabledChanged));

    public static void SetEnabled(DependencyObject element, bool value) => element.SetValue(EnabledProperty, value);
    public static bool GetEnabled(DependencyObject element) => (bool)element.GetValue(EnabledProperty);

    private static void OnEnabledChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is not ComboBox combo)
            return;

        combo.DropDownOpened -= OnDropDownOpened;
        combo.DropDownClosed -= OnDropDownClosed;

        if ((bool)e.NewValue)
        {
            combo.DropDownOpened += OnDropDownOpened;
            combo.DropDownClosed += OnDropDownClosed;
        }
    }

    private static void OnDropDownOpened(object? sender, EventArgs e)
    {
        var combo = (ComboBox)sender!;
        if (combo.Template.FindName("PART_SearchBox", combo) is not TextBox box)
            return;

        // Wire the handler once per box; Tag flags that we've already subscribed.
        if (!ReferenceEquals(box.Tag, combo))
        {
            box.Tag = combo;
            box.TextChanged += (_, _) => ApplyFilter(combo, box.Text);
        }

        box.Text = "";            // start every open with the full list
        ApplyFilter(combo, "");
        box.Focus();
    }

    private static void OnDropDownClosed(object? sender, EventArgs e) =>
        ApplyFilter((ComboBox)sender!, "");

    private static void ApplyFilter(ComboBox combo, string? text)
    {
        if (CollectionViewSource.GetDefaultView(combo.ItemsSource) is not { } view)
            return;

        var query = text?.Trim();
        view.Filter = string.IsNullOrEmpty(query)
            ? null
            : o => o is WarehouseProductRow row &&
                   (row.Name.Contains(query, StringComparison.OrdinalIgnoreCase) ||
                    row.Sku.Contains(query, StringComparison.OrdinalIgnoreCase));
    }
}