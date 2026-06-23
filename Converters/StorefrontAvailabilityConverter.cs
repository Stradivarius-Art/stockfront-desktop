using System;
using System.Collections.Generic;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;
using StockFront.Contracts.Storefront;

namespace stockfront.Converters;

/// <summary>
/// Maps a <see cref="StorefrontAvailability"/> to its storefront-card visuals. <c>ConverterParameter</c>
/// selects the part: <c>Label</c> (the Russian caption next to the price) or <c>Foreground</c> (default)
/// — a muted traffic light, green/amber/red. The "В корзину" button's enabled state is handled in XAML
/// by a trigger on the availability itself.
/// </summary>
public sealed class StorefrontAvailabilityConverter : IValueConverter
{
    private sealed record Visual(string Foreground, string Label);

    private static readonly IReadOnlyDictionary<StorefrontAvailability, Visual> Map =
        new Dictionary<StorefrontAvailability, Visual>
        {
            [StorefrontAvailability.InStock]    = new("#3F9D52", "в наличии"),
            [StorefrontAvailability.Low]        = new("#B8862E", "мало"),
            [StorefrontAvailability.OutOfStock] = new("#D24B4B", "нет в наличии"),
        };

    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is not StorefrontAvailability availability || !Map.TryGetValue(availability, out var v))
            return Binding.DoNothing;

        return (parameter as string) switch
        {
            "Label" => v.Label,
            _ => Brush(v.Foreground)
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