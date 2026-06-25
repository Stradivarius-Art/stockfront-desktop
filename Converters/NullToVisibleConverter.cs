using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace stockfront.Converters;

/// <summary>
/// Shows an element only when its bound value is null or an empty/whitespace string, otherwise collapses
/// it — the inverse of <see cref="NullToCollapsedConverter"/>. Used to show a product's placeholder icon
/// only while it has no image, so the icon doesn't peek through a scaled (Uniform) photo's gaps.
/// </summary>
public sealed class NullToVisibleConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        value is null || (value is string s && string.IsNullOrWhiteSpace(s))
            ? Visibility.Visible
            : Visibility.Collapsed;

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}