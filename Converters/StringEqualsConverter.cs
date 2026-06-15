using System.Globalization;
using System.Windows.Data;

namespace stockfront.Converters;

/// <summary>
/// True when the bound value equals the <see cref="Binding.ConverterParameter"/> (string compare).
/// Used to light up the active sidebar item by comparing the open section to each button's name.
/// One-way only — the RadioButton group and the navigation command drive the selection.
/// </summary>
public sealed class StringEqualsConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        string.Equals(value?.ToString(), parameter?.ToString(), StringComparison.Ordinal);

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}
