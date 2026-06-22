using System;
using System.Collections.Generic;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;
using StockFront.Contracts.Dashboard;

namespace stockfront.Converters;

/// <summary>
/// Maps a <see cref="StockStatus"/> to its badge visuals. <c>ConverterParameter</c> selects which
/// part: <c>Background</c> (default), <c>Foreground</c>, or <c>Label</c> (the Russian caption). Keeps
/// the nine-status traffic light in one place instead of a wall of DataTriggers in every view.
/// </summary>
public sealed class StockStatusConverter : IValueConverter
{
    private sealed record Visual(string Background, string Foreground, string Label);

    // Soft pastel fill + saturated text, one row per status (see CLAUDE.md palette).
    private static readonly IReadOnlyDictionary<StockStatus, Visual> Map = new Dictionary<StockStatus, Visual>
    {
        [StockStatus.OutOfStock]  = new("#F8D7D7", "#D24B4B", "нет"),
        [StockStatus.Critical]    = new("#FBE0CC", "#C9551F", "критично"),
        [StockStatus.Low]         = new("#F7EAC9", "#B8862E", "мало"),
        [StockStatus.Normal]      = new("#D9F0DD", "#3F9D52", "норма"),
        [StockStatus.AboveNormal] = new("#D3ECE8", "#2E8B84", "выше нормы"),
        [StockStatus.Overstock]   = new("#D6E4F7", "#3B6FB0", "избыток"),
        [StockStatus.New]         = new("#ECDFF7", "#7E4FB0", "новинка"),
        [StockStatus.Illiquid]    = new("#E7E4E0", "#7A756E", "неликвид"),
        [StockStatus.Seasonal]    = new("#EDE7F5", "#6B6B6B", "сезон"),
    };

    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is not StockStatus status || !Map.TryGetValue(status, out var v))
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