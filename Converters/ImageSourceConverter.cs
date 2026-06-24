using System.Globalization;
using System.IO;
using System.Windows.Data;
using System.Windows.Media.Imaging;
using stockfront.Infrastructure;

namespace stockfront.Converters;

/// <summary>
/// Turns a stored product image name (relative to <see cref="ImageStore"/>) into a WPF image source.
/// Loads the bytes eagerly (<see cref="BitmapCacheOption.OnLoad"/>) so the file isn't locked and can be
/// replaced later. Returns <c>null</c> when there's no image, so the card shows its placeholder.
/// </summary>
public sealed class ImageSourceConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        var path = ImageStore.Resolve(value as string);
        if (path is null)
            return null;

        try
        {
            var image = new BitmapImage();
            image.BeginInit();
            image.CacheOption = BitmapCacheOption.OnLoad;
            image.CreateOptions = BitmapCreateOptions.IgnoreImageCache;
            image.UriSource = new Uri(path, UriKind.Absolute);
            image.EndInit();
            image.Freeze();
            return image;
        }
        catch (Exception ex) when (ex is IOException or NotSupportedException or UriFormatException)
        {
            // A corrupt or unreadable file shouldn't crash the catalog — just fall back to placeholder.
            return null;
        }
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}