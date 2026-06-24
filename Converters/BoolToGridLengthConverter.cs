using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace stockfront.Converters;

/// <summary>
/// Maps a boolean to a <see cref="GridLength"/>: <c>true</c> → a fixed width taken from
/// <c>ConverterParameter</c> (in device-independent pixels, e.g. "88"), <c>false</c> → zero width.
/// Used to collapse a whole grid column when a feature is off — e.g. the admin-only row-actions
/// column in the warehouse table, so non-admins don't see an empty gap.
/// </summary>
public sealed class BoolToGridLengthConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        var on = value is true;
        if (!on)
            return new GridLength(0);

        var width = parameter is string s
                    && double.TryParse(s, NumberStyles.Number, CultureInfo.InvariantCulture, out var w)
            ? w
            : 0;
        return new GridLength(width);
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}
