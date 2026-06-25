namespace StockFront.Contracts.Orders;

/// <summary>
/// The Orders module's contract: turn a storefront cart into a saved order, reserving the goods in the
/// warehouse in the same transaction (see CLAUDE.md "Storefront → Orders" / "Orders → Warehouse"). The
/// reservation becomes a real write-off only on shipment; cancellation releases it. Validates input and
/// reports business-rule violations through <see cref="OrderResult"/> rather than throwing.
/// </summary>
public interface IOrderService
{
    /// <summary>
    /// Place an order for <paramref name="customerName"/> from the given cart <paramref name="items"/>:
    /// re-check availability, reserve the goods in the warehouse and persist the order as Новая with its
    /// lines, all or nothing. Fails (changing nothing) if the cart is empty or any line exceeds stock.
    /// </summary>
    Task<OrderResult> PlaceOrderAsync(
        string customerName, IReadOnlyList<CartItem> items, CancellationToken ct = default);

    /// <summary>
    /// List orders (newest first). Pass a customer name to restrict the list to that customer's own
    /// orders (purchasers only see their own); pass <c>null</c> for the full list (admin / staff).
    /// </summary>
    Task<IReadOnlyList<OrderRow>> GetOrdersAsync(string? customerName = null, CancellationToken ct = default);

    /// <summary>
    /// Advance order <paramref name="orderId"/> to <paramref name="newStatus"/>, validating the
    /// transition against the lifecycle (New → Reserved → Paid → Shipped → Completed, plus Cancelled
    /// from any non-terminal state). Fails (changing nothing) on an illegal transition or unknown order.
    /// </summary>
    Task<OrderResult> ChangeStatusAsync(int orderId, OrderStatus newStatus, CancellationToken ct = default);
}