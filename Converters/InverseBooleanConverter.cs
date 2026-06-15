using System.Globalization;
using System.Windows.Data;

namespace stockfront.Converters;

/// <summary>Negates a boolean. Used to bind the two "existing / new product" radio buttons to a
/// single <c>ReceiveIsNewProduct</c> flag.</summary>
public sealed class InverseBooleanConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        value is bool b && !b;

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        value is bool b && !b;
}