using System;
using System.Collections.Generic;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;
using StockFront.Contracts.Orders;

namespace stockfront.Converters;

/// <summary>
/// Maps an <see cref="OrderStatus"/> to its badge visuals. <c>ConverterParameter</c> selects which
/// part: <c>Background</c> (default), <c>Foreground</c>, or <c>Label</c> (the Russian caption). Keeps
/// the order-status traffic light in one place instead of DataTriggers spread across the view.
/// </summary>
public sealed class OrderStatusConverter : IValueConverter
{
    private sealed record Visual(string Background, string Foreground, string Label);

    // Soft pastel fill + saturated text, one row per state (see CLAUDE.md palette).
    private static readonly IReadOnlyDictionary<OrderStatus, Visual> Map = new Dictionary<OrderStatus, Visual>
    {
        [OrderStatus.New]       = new("#D6E4F7", "#3B6FB0", "Новая"),
        [OrderStatus.Reserved]  = new("#FBE0CC", "#C9551F", "Зарезервирована"),
        [OrderStatus.Paid]      = new("#ECDFF7", "#7E4FB0", "Оплачена"),
        [OrderStatus.Shipped]   = new("#D9F0DD", "#3F9D52", "Отгружена"),
        [OrderStatus.Completed] = new("#D3ECE8", "#2E8B84", "Завершена"),
        [OrderStatus.Cancelled] = new("#F8D7D7", "#D24B4B", "Отменена"),
    };

    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is not OrderStatus status || !Map.TryGetValue(status, out var v))
            return Binding.DoNothing;

        return (parameter as string) switch
        {
            "Foreground" => Brush(v.Foreground),
            "Label" => v.Label,
            _ => Brush(v.Background)
        };
    }

    private static readonly Dictionary<string, SolidColorBrush> BrushCache = new();

    private static SolidColorBrush Brush(string hex)
    {
        if (BrushCache.TryGetValue(hex, out var cached))
            return cached;

        var brush = new SolidColorBrush((Color)ColorConverter.ConvertFromString(hex));
        brush.Freeze();
        BrushCache[hex] = brush;
        return brush;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}