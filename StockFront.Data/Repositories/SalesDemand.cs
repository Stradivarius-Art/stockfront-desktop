using Microsoft.EntityFrameworkCore;
using StockFront.Data.Entities;

namespace StockFront.Data.Repositories;

/// <summary>
/// Per-product demand signals used to derive availability (Days of Supply): units sold within the
/// averaging window, and the date of the most recent sale. Loaded once with two grouped queries and
/// shared by the warehouse table and the dashboard so they can't drift apart. "Sales" are order lines
/// of shipped or completed orders.
/// </summary>
internal sealed class SalesDemand
{
    /// <summary>Averaging window for daily sales, in days. The single place this is defined.</summary>
    public const int WindowDays = 30;

    private readonly IReadOnlyDictionary<int, int> _soldInWindow;
    private readonly IReadOnlyDictionary<int, DateTime?> _lastSale;

    private SalesDemand(
        IReadOnlyDictionary<int, int> soldInWindow,
        IReadOnlyDictionary<int, DateTime?> lastSale)
    {
        _soldInWindow = soldInWindow;
        _lastSale = lastSale;
    }

    public static async Task<SalesDemand> LoadAsync(AppDbContext db, DateTime now, CancellationToken ct = default)
    {
        var windowStart = now.AddDays(-WindowDays);

        var soldInWindow = await db.OrderLines.AsNoTracking()
            .Where(l => (l.Order.Status == OrderStatus.Shipped || l.Order.Status == OrderStatus.Completed)
                        && l.Order.CreatedAt >= windowStart)
            .GroupBy(l => l.ProductId)
            .Select(g => new { ProductId = g.Key, Qty = g.Sum(x => x.Quantity) })
            .ToDictionaryAsync(x => x.ProductId, x => x.Qty, ct);

        var lastSale = await db.OrderLines.AsNoTracking()
            .Where(l => l.Order.Status == OrderStatus.Shipped || l.Order.Status == OrderStatus.Completed)
            .GroupBy(l => l.ProductId)
            .Select(g => new { ProductId = g.Key, Last = g.Max(x => (DateTime?)x.Order.CreatedAt) })
            .ToDictionaryAsync(x => x.ProductId, x => x.Last, ct);

        return new SalesDemand(soldInWindow, lastSale);
    }

    /// <summary>Average units sold per day over the window (0 when there were no sales).</summary>
    public double AvgDailySales(int productId) =>
        (_soldInWindow.TryGetValue(productId, out var qty) ? qty : 0) / (double)WindowDays;

    /// <summary>Date of the most recent sale, or <c>null</c> if the product was never sold.</summary>
    public DateTime? LastSale(int productId) =>
        _lastSale.TryGetValue(productId, out var last) ? last : null;
}