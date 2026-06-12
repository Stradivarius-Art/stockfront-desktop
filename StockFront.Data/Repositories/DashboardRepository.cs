using Microsoft.EntityFrameworkCore;
using StockFront.Contracts.Dashboard;
using StockFront.Data.Entities;

namespace StockFront.Data.Repositories;

/// <summary>
/// Builds the warehouse dashboard read model from the database. All queries are read-only
/// (<c>AsNoTracking</c>) and asynchronous.
/// </summary>
public sealed class DashboardRepository : IDashboardRepository
{
    // At or below this many available units a product counts as "low". A temporary home for the
    // threshold — it belongs to the Warehouse module once that exists.
    private const int LowStockThreshold = 10;

    private readonly AppDbContext _db;

    public DashboardRepository(AppDbContext db) => _db = db;

    public async Task<DashboardSnapshot> GetSnapshotAsync(CancellationToken ct = default)
    {
        var now = DateTime.UtcNow;
        var monthStart = new DateTime(now.Year, now.Month, 1, 0, 0, 0, DateTimeKind.Utc);

        var totalUnits = await _db.StockItems.AsNoTracking()
            .SumAsync(s => (int?)s.Quantity, ct) ?? 0;

        var activeOrders = await _db.Orders.AsNoTracking()
            .CountAsync(o => o.Status != OrderStatus.Completed && o.Status != OrderStatus.Cancelled, ct);

        var lowStockCount = await _db.StockItems.AsNoTracking()
            .CountAsync(s => s.Quantity - s.Reserved <= LowStockThreshold, ct);

        var monthRevenue = await _db.OrderLines.AsNoTracking()
            .Where(l => l.Order.CreatedAt >= monthStart
                        && (l.Order.Status == OrderStatus.Paid
                            || l.Order.Status == OrderStatus.Shipped
                            || l.Order.Status == OrderStatus.Completed))
            .SumAsync(l => (decimal?)(l.Quantity * l.UnitPrice), ct) ?? 0m;

        var rows = await _db.Products.AsNoTracking()
            .OrderBy(p => p.Name)
            .Select(p => new
            {
                p.Name,
                p.Sku,
                Quantity = p.Stock != null ? p.Stock.Quantity : 0,
                Available = p.Stock != null ? p.Stock.Quantity - p.Stock.Reserved : 0
            })
            .ToListAsync(ct);

        var products = rows
            .Select(r => new DashboardProductRow(r.Name, r.Sku, r.Quantity, Classify(r.Available)))
            .ToList();

        return new DashboardSnapshot(totalUnits, activeOrders, lowStockCount, monthRevenue, products);
    }

    private static StockStatus Classify(int available) =>
        available <= 0 ? StockStatus.OutOfStock
        : available <= LowStockThreshold ? StockStatus.Low
        : StockStatus.InStock;
}
