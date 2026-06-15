using System.Globalization;
using System.Windows.Data;

namespace stockfront.Converters;

/// <summary>
/// True when the two bound values are equal. Used to light up the active filter chip by comparing
/// the chip's own value to the current <c>ActiveFilter</c> (a value-to-value compare that a single
/// binding can't do). One-way.
/// </summary>
public sealed class EqualityConverter : IMultiValueConverter
{
    public object Convert(object[] values, Type targetType, object? parameter, CultureInfo culture) =>
        values.Length == 2 && Equals(values[0], values[1]);

    public object[] ConvertBack(object value, Type[] targetTypes, object? parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}