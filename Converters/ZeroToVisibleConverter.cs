using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace stockfront.Converters;

/// <summary>
/// Visible when the bound number is zero, otherwise Collapsed. Used to offer the delete-product action
/// only for a product whose stock has reached zero.
/// </summary>
public sealed class ZeroToVisibleConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        var isZero = value switch
        {
            int i => i == 0,
            long l => l == 0,
            double d => d == 0,
            decimal m => m == 0,
            _ => false
        };
        return isZero ? Visibility.Visible : Visibility.Collapsed;
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}