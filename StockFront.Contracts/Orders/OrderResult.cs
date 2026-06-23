namespace StockFront.Contracts.Orders;

/// <summary>
/// Outcome of placing an order. On success carries the new <see cref="OrderId"/>; on failure
/// <see cref="Error"/> holds a short user-facing message (e.g. a product ran out between browsing and
/// checkout).
/// </summary>
public sealed record OrderResult(bool Succeeded, int OrderId, string? Error)
{
    public static OrderResult Ok(int orderId) => new(true, orderId, null);

    public static OrderResult Fail(string error) => new(false, 0, error);
}