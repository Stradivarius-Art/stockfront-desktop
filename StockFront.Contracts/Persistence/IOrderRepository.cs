using StockFront.Contracts.Orders;

namespace StockFront.Contracts.Persistence;

/// <summary>
/// Data-layer access for the orders module. Pure persistence — the <see cref="IOrderService"/> does the
/// input validation; this performs the all-or-nothing checkout against the database.
/// </summary>
public interface IOrderRepository
{
    /// <summary>
    /// In a single transaction: re-check stock for every line, reserve it (Reserved += qty, physical
    /// Quantity untouched), and save the order with its lines as Новая. Returns the new order id, or a
    /// failure result if a line can no longer be satisfied. The reservation becomes a real write-off on
    /// shipment (see <see cref="UpdateStatusAsync"/>). <paramref name="performedBy"/> stamps the journal.
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
    /// Set the order's status, applying the stock side effects of the transition in one transaction:
    /// shipment turns the reservation into a physical write-off (and journals a sale), cancellation
    /// releases the reservation. Returns <c>false</c> without changing anything if the order does not
    /// exist. Which transitions are legal and who may make them are enforced by the service above.
    /// </summary>
    Task<bool> UpdateStatusAsync(int orderId, OrderStatus status, CancellationToken ct = default);
}