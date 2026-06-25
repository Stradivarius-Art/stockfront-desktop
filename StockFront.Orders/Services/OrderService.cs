using StockFront.Contracts.Orders;
using StockFront.Contracts.Persistence;

namespace StockFront.Orders.Services;

/// <summary>
/// Orders business logic. Validates the cart (a customer, at least one line, positive quantities) and
/// hands the checkout to <see cref="IOrderRepository"/>, which deducts the goods from the warehouse and
/// saves the order in one transaction. The availability re-check that matters happens there, at commit
/// time, against live stock — this class never touches the database, so it unit-tests with a mocked repo.
/// </summary>
public sealed class OrderService : IOrderService
{
    private readonly IOrderRepository _repo;

    public OrderService(IOrderRepository repo) => _repo = repo;

    public Task<OrderResult> PlaceOrderAsync(
        string customerName, IReadOnlyList<CartItem> items, CancellationToken ct = default)
    {
        customerName = customerName?.Trim() ?? "";

        if (customerName.Length == 0)
            return Task.FromResult(OrderResult.Fail("Не указан покупатель."));
        if (items is null || items.Count == 0)
            return Task.FromResult(OrderResult.Fail("Корзина пуста."));
        if (items.Any(i => i.Quantity <= 0))
            return Task.FromResult(OrderResult.Fail("Количество в каждой позиции должно быть больше нуля."));

        // The order is stamped with the customer as the journal's performer for its sale movements.
        return _repo.PlaceOrderAsync(customerName, items, customerName, ct);
    }

    public Task<IReadOnlyList<OrderRow>> GetOrdersAsync(
        string? customerName = null, CancellationToken ct = default) =>
        _repo.GetOrdersAsync(string.IsNullOrWhiteSpace(customerName) ? null : customerName.Trim(), ct);

    public async Task<OrderResult> ChangeStatusAsync(
        int orderId, OrderStatus newStatus, CancellationToken ct = default)
    {
        var rows = await _repo.GetOrdersAsync(null, ct);
        var order = rows.FirstOrDefault(o => o.Id == orderId);
        if (order is null)
            return OrderResult.Fail("Заказ не найден.");

        if (!IsLegalTransition(order.Status, newStatus))
            return OrderResult.Fail("Недопустимое изменение статуса заказа.");

        return await _repo.UpdateStatusAsync(orderId, newStatus, ct)
            ? OrderResult.Ok(orderId)
            : OrderResult.Fail("Заказ не найден.");
    }

    /// <summary>
    /// The order lifecycle (see diagrams/diagram_state.png): forward one step along
    /// New → Reserved → Paid → Shipped → Completed, or cancel from any non-terminal state.
    /// </summary>
    private static bool IsLegalTransition(OrderStatus from, OrderStatus to) => (from, to) switch
    {
        (OrderStatus.New, OrderStatus.Reserved) => true,
        (OrderStatus.Reserved, OrderStatus.Paid) => true,
        (OrderStatus.Paid, OrderStatus.Shipped) => true,
        (OrderStatus.Shipped, OrderStatus.Completed) => true,

        // Cancel is allowed before the goods leave the warehouse, never from a terminal state.
        (OrderStatus.New or OrderStatus.Reserved or OrderStatus.Paid, OrderStatus.Cancelled) => true,

        _ => false
    };
}