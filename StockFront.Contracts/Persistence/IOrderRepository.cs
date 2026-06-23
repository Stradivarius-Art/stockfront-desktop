using StockFront.Contracts.Orders;

namespace StockFront.Contracts.Persistence;

/// <summary>
/// Data-layer access for the orders module. Pure persistence — the <see cref="IOrderService"/> does the
/// input validation; this performs the all-or-nothing checkout against the database.
/// </summary>
public interface IOrderRepository
{
    /// <summary>
    /// In a single transaction: re-check stock for every line, decrement it (journaling a sale
    /// movement per product), and save the order with its lines. Returns the new order id, or a failure
    /// result if a line can no longer be satisfied. <paramref name="performedBy"/> stamps the journal.
    /// </summary>
    Task<OrderResult> PlaceOrderAsync(
        string customerName, IReadOnlyList<CartItem> items, string? performedBy, CancellationToken ct = default);
}