using System.IO;

namespace stockfront.Infrastructure;

/// <summary>
/// Local store for product images. Picked files are copied into a per-user application folder and the
/// database keeps only the relative file name, so images survive moving the database but stay outside
/// it (see the storefront card editor). A desktop app has no web host to serve files, so this is the
/// simplest durable option.
/// </summary>
public static class ImageStore
{
    /// <summary>The folder that holds the copied product images (created on first use).</summary>
    public static string BaseDirectory { get; } = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "StockFront", "images");

    /// <summary>
    /// Copy <paramref name="sourcePath"/> into the store under a fresh unique name and return that
    /// relative name (to persist). Throws if the source can't be read.
    /// </summary>
    public static string Save(string sourcePath)
    {
        Directory.CreateDirectory(BaseDirectory);

        var extension = Path.GetExtension(sourcePath);
        var fileName = $"{Guid.NewGuid():N}{extension}";
        File.Copy(sourcePath, Path.Combine(BaseDirectory, fileName), overwrite: true);
        return fileName;
    }

    /// <summary>
    /// Resolve a stored relative name to an absolute path, or <c>null</c> if there's no image or the
    /// file is missing. Used by the image converter to feed WPF an existing path.
    /// </summary>
    public static string? Resolve(string? relativeName)
    {
        if (string.IsNullOrWhiteSpace(relativeName))
            return null;

        var full = Path.Combine(BaseDirectory, relativeName);
        return File.Exists(full) ? full : null;
    }
}