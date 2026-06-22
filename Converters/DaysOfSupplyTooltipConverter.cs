using System;
using System.Globalization;
using System.Windows.Data;

namespace stockfront.Converters;

/// <summary>
/// Formats a product's Days of Supply (<c>double?</c>) into a badge tooltip, e.g. "Запас ≈ 23 дн.".
/// A <c>null</c> value means there were no recent sales to divide by — shown as "нет продаж за период".
/// </summary>
public sealed class DaysOfSupplyTooltipConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture) =>
        value is double dos
            ? $"Запас ≈ {Math.Round(dos)} дн. продаж"
            : "Запас: нет продаж за период";

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}