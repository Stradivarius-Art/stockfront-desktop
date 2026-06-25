using Microsoft.EntityFrameworkCore;
using StockFront.Contracts.Orders;
using StockFront.Contracts.Persistence;
using StockFront.Data.Entities;

namespace StockFront.Data.Repositories;

/// <summary>
/// EF Core implementation of <see cref="IOrderRepository"/>. Checkout is one transaction: every line is
/// re-checked against current stock and the goods are <em>reserved</em> (Reserved += qty) — the order is
/// saved as Новая, physical stock is untouched until shipment. If any line can't be satisfied the whole
/// thing rolls back and nothing changes — the catalog a customer browsed may be a few seconds stale, so
/// the authoritative check happens here at commit time. The reservation is later turned into a real
/// write-off (a <see cref="StockMovementType.Sale"/> journal entry) on shipment, or released on cancel,
/// in <see cref="UpdateStatusAsync"/> — see diagrams/diagram_state.png.
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

            // A new order starts as Новая and reserves the goods (Reserved += qty) without touching the
            // physical Quantity. The reservation is held through Paid; shipment converts it to a real
            // write-off, cancellation releases it — see UpdateStatusAsync and diagrams/diagram_state.png.
            var order = new Order { CustomerName = customerName, Status = OrderStatus.New };

            foreach (var (productId, quantity) in requested)
            {
                var product = products.FirstOrDefault(p => p.Id == productId);
                if (product is null)
                    return OrderResult.Fail("Товар из корзины больше не доступен.");

                var available = (product.Stock?.Quantity ?? 0) - (product.Stock?.Reserved ?? 0);
                if (quantity > available)
                    return OrderResult.Fail($"«{product.Name}»: доступно только {available} шт.");

                product.Stock!.Reserved += quantity;

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
        var order = await _db.Orders
            .Include(o => o.Lines)
            .FirstOrDefaultAsync(o => o.Id == orderId, ct);
        if (order is null)
            return false;

        await using var tx = await _db.Database.BeginTransactionAsync(ct);
        try
        {
            // Stock side effects of the transition (the legality of the transition itself is enforced by
            // the service above). The reservation made at placement is held through Paid; here it is either
            // converted to a physical write-off on shipment, or released on cancellation.
            if (status is OrderStatus.Shipped or OrderStatus.Cancelled && order.Lines.Count > 0)
            {
                var ids = order.Lines.Select(l => l.ProductId).ToList();
                var stocks = await _db.StockItems
                    .Where(s => ids.Contains(s.ProductId))
                    .ToDictionaryAsync(s => s.ProductId, ct);

                foreach (var line in order.Lines)
                {
                    if (!stocks.TryGetValue(line.ProductId, out var stock))
                        continue;

                    if (status == OrderStatus.Shipped)
                    {
                        // Goods leave the warehouse: drop physical stock and release the reservation,
                        // journaling the sale so the movement report sees it.
                        stock.Quantity -= line.Quantity;
                        stock.Reserved -= line.Quantity;

                        _db.StockMovements.Add(new StockMovement
                        {
                            ProductId = line.ProductId,
                            Type = StockMovementType.Sale,
                            Quantity = -line.Quantity,
                            Comment = $"Отгрузка по заказу №{order.Id}: {order.CustomerName}",
                            PerformedBy = order.CustomerName
                        });
                    }
                    else // Cancelled: release the reservation; physical stock was never touched.
                    {
                        stock.Reserved -= line.Quantity;
                    }
                }
            }

            order.Status = status;
            await _db.SaveChangesAsync(ct);
            await tx.CommitAsync(ct);
            return true;
        }
        catch
        {
            await tx.RollbackAsync(ct);
            throw;
        }
    }
}