using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace stockfront.Converters;

/// <summary>
/// <c>true</c> → <see cref="Visibility.Collapsed"/>, <c>false</c> → <see cref="Visibility.Visible"/>.
/// The inverse of the built-in BooleanToVisibilityConverter — used for "empty state" panels that show
/// only when a flag is off (e.g. "Корзина пуста" when the cart has no items).
/// </summary>
public sealed class InverseBoolToVisibilityConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        value is bool b && b ? Visibility.Collapsed : Visibility.Visible;

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        value is Visibility v && v != Visibility.Visible;
}