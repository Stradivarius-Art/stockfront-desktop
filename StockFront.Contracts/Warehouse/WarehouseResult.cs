namespace StockFront.Contracts.Warehouse;

/// <summary>
/// Outcome of a warehouse operation (receive / write-off). On failure <see cref="Error"/> holds a
/// short user-facing message (e.g. "недостаточно товара на складе").
/// </summary>
public sealed record WarehouseResult(bool Succeeded, string? Error)
{
    public static WarehouseResult Ok() => new(true, null);

    public static WarehouseResult Fail(string error) => new(false, error);
}
