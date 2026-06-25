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

    /// <summary>
    /// All orders as read rows (newest first), each with its line count and total. When
    /// <paramref name="customerName"/> is given, only that customer's orders are returned (a customer
    /// sees only their own — see the role matrix in CLAUDE.md).
    /// </summary>
    Task<IReadOnlyList<OrderRow>> GetOrdersAsync(string? customerName, CancellationToken ct = default);

    /// <summary>
    /// Set the order's status. Returns <c>false</c> without changing anything if the order does not
    /// exist. The lifecycle rules (which transitions are legal, who may make them) are enforced by the
    /// service above; this only persists the new state.
    /// </summary>
    Task<bool> UpdateStatusAsync(int orderId, OrderStatus status, CancellationToken ct = default);
}