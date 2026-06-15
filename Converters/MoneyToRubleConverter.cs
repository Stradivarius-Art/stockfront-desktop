using System.Globalization;
using System.Windows.Data;

namespace stockfront.Converters;

/// <summary>
/// Formats a <see cref="decimal"/> money value as roubles with thinned thousands, e.g.
/// 4990 → "4 990 ₽". One-way: display only.
/// </summary>
public sealed class MoneyToRubleConverter : IValueConverter
{
    private static readonly CultureInfo Ru = CultureInfo.GetCultureInfo("ru-RU");

    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        value is decimal money ? $"{money.ToString("N0", Ru)} ₽" : "";

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}