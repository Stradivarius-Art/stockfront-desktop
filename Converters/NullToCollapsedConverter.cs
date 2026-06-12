using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace stockfront.Converters;

/// <summary>
/// Collapses an element when its bound value is null or an empty/whitespace string, otherwise
/// shows it. Used to hide the login error line until there is a message.
/// </summary>
public sealed class NullToCollapsedConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        value is null || (value is string s && string.IsNullOrWhiteSpace(s))
            ? Visibility.Collapsed
            : Visibility.Visible;

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}
