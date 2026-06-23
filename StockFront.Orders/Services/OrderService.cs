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
}