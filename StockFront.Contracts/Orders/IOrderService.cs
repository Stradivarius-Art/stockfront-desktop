namespace StockFront.Contracts.Orders;

/// <summary>
/// The Orders module's contract: turn a storefront cart into a saved order, deducting the goods from
/// the warehouse in the same transaction (see CLAUDE.md "Storefront → Orders" / "Orders → Warehouse").
/// Validates input and reports business-rule violations through <see cref="OrderResult"/> rather than
/// throwing.
/// </summary>
public interface IOrderService
{
    /// <summary>
    /// Place an order for <paramref name="customerName"/> from the given cart <paramref name="items"/>:
    /// re-check availability, write the goods off the warehouse and persist the order with its lines,
    /// all or nothing. Fails (changing nothing) if the cart is empty or any line exceeds stock.
    /// </summary>
    Task<OrderResult> PlaceOrderAsync(
        string customerName, IReadOnlyList<CartItem> items, CancellationToken ct = default);
}