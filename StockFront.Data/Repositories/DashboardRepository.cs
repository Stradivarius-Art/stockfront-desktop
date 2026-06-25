using Microsoft.EntityFrameworkCore;
using StockFront.Contracts.Dashboard;
using StockFront.Contracts.Orders;
using StockFront.Contracts.Warehouse;
using StockFront.Data.Entities;

namespace StockFront.Data.Repositories;

/// <summary>
/// Builds the warehouse dashboard read model from the database. Availability is derived with the same
/// <see cref="StockStatusCalculator"/> and <see cref="SalesDemand"/> the warehouse module uses, so the
/// dashboard and the warehouse table never disagree. All queries are read-only (<c>AsNoTracking</c>)
/// and asynchronous.
/// </summary>
public sealed class DashboardRepository : IDashboardRepository
{
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

        var monthRevenue = await _db.OrderLines.AsNoTracking()
            .Where(l => l.Order.CreatedAt >= monthStart
                        && (l.Order.Status == OrderStatus.Paid
                            || l.Order.Status == OrderStatus.Shipped
                            || l.Order.Status == OrderStatus.Completed))
            .SumAsync(l => (decimal?)(l.Quantity * l.UnitPrice), ct) ?? 0m;

        var demand = await SalesDemand.LoadAsync(_db, now, ct);

        var rows = await _db.Products.AsNoTracking()
            .OrderBy(p => p.Name)
            .Select(p => new
            {
                p.Id,
                p.Name,
                p.Sku,
                Quantity = p.Stock != null ? p.Stock.Quantity : 0,
                p.CreatedAt,
                p.LeadTimeDays,
                p.SafetyBufferDays,
                p.IsSeasonal
            })
            .ToListAsync(ct);

        var products = rows
            .Select(r => new DashboardProductRow(r.Name, r.Sku, r.Quantity,
                StockStatusCalculator.Classify(new StockStatusInputs(
                    Quantity: r.Quantity,
                    AvgDailySales: demand.AvgDailySales(r.Id),
                    LastSale: demand.LastSale(r.Id),
                    CreatedAt: r.CreatedAt,
                    LeadTimeDays: r.LeadTimeDays,
                    SafetyBufferDays: r.SafetyBufferDays,
                    IsSeasonal: r.IsSeasonal,
                    Now: now))))
            .ToList();

        var lowStockCount = products.Count(p =>
            p.Status is StockStatus.OutOfStock or StockStatus.Critical or StockStatus.Low);

        return new DashboardSnapshot(totalUnits, activeOrders, lowStockCount, monthRevenue, products);
    }
}