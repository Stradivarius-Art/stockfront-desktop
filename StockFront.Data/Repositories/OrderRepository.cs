using Microsoft.EntityFrameworkCore;
using StockFront.Contracts.Orders;
using StockFront.Contracts.Persistence;
using StockFront.Data.Entities;

namespace StockFront.Data.Repositories;

/// <summary>
/// EF Core implementation of <see cref="IOrderRepository"/>. Checkout is one transaction: every line is
/// re-checked against current stock, the goods are written off (each a <see cref="StockMovementType.Sale"/>
/// journal entry so the movement report sees the sale), and the <see cref="Order"/> with its lines is
/// saved. If any line can't be satisfied the whole thing rolls back and nothing changes — the catalog a
/// customer browsed may be a few seconds stale, so the authoritative check happens here at commit time.
/// </summary>
public sealed class OrderRepository : IOrderRepository
{
    private readonly AppDbContext _db;

    public OrderRepository(AppDbContext db) => _db = db;

    public async Task<OrderResult> PlaceOrderAsync(
        string customerName, IReadOnlyList<CartItem> items, string? performedBy, CancellationToken ct = default)
    {
        if (items.Count == 0)
            return OrderResult.Fail("Корзина пуста.");

        // Collapse duplicate product lines so a product can't slip past the stock check across two lines.
        var requested = items
            .GroupBy(i => i.ProductId)
            .ToDictionary(g => g.Key, g => g.Sum(i => i.Quantity));

        if (requested.Values.Any(q => q <= 0))
            return OrderResult.Fail("Количество в каждой позиции должно быть больше нуля.");

        await using var tx = await _db.Database.BeginTransactionAsync(ct);
        try
        {
            var ids = requested.Keys.ToList();
            var products = await _db.Products
                .Include(p => p.Stock)
                .Where(p => ids.Contains(p.Id))
                .ToListAsync(ct);

            var order = new Order { CustomerName = customerName, Status = OrderStatus.Completed };

            foreach (var (productId, quantity) in requested)
            {
                var product = products.FirstOrDefault(p => p.Id == productId);
                if (product is null)
                    return OrderResult.Fail("Товар из корзины больше не доступен.");

                var available = (product.Stock?.Quantity ?? 0) - (product.Stock?.Reserved ?? 0);
                if (quantity > available)
                    return OrderResult.Fail($"«{product.Name}»: доступно только {available} шт.");

                product.Stock!.Quantity -= quantity;

                _db.StockMovements.Add(new StockMovement
                {
                    ProductId = productId,
                    Type = StockMovementType.Sale,
                    Quantity = -quantity,
                    Comment = $"Продажа по заказу: {customerName}",
                    PerformedBy = performedBy
                });

                order.Lines.Add(new OrderLine
                {
                    ProductId = productId,
                    Quantity = quantity,
                    UnitPrice = product.Price
                });
            }

            _db.Orders.Add(order);
            await _db.SaveChangesAsync(ct);
            await tx.CommitAsync(ct);

            return OrderResult.Ok(order.Id);
        }
        catch
        {
            await tx.RollbackAsync(ct);
            throw;
        }
    }

    public async Task<IReadOnlyList<OrderRow>> GetOrdersAsync(string? customerName, CancellationToken ct = default)
    {
        var query = _db.Orders.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(customerName))
            query = query.Where(o => o.CustomerName == customerName);

        return await query
            .OrderByDescending(o => o.CreatedAt)
            .ThenByDescending(o => o.Id)
            .Select(o => new OrderRow(
                o.Id,
                o.CustomerName,
                o.CreatedAt,
                o.Lines.Count,
                o.Lines.Sum(l => l.UnitPrice * l.Quantity),
                o.Status))
            .ToListAsync(ct);
    }

    public async Task<bool> UpdateStatusAsync(int orderId, OrderStatus status, CancellationToken ct = default)
    {
        var order = await _db.Orders.FirstOrDefaultAsync(o => o.Id == orderId, ct);
        if (order is null)
            return false;

        order.Status = status;
        await _db.SaveChangesAsync(ct);
        return true;
    }
}