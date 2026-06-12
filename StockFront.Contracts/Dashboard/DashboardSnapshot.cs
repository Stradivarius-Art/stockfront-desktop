namespace StockFront.Contracts.Dashboard;

/// <summary>
/// Everything the warehouse dashboard shows in one read: the four metric cards and the products
/// table. A read model assembled by <see cref="IDashboardRepository"/> — no domain behaviour.
/// </summary>
public sealed record DashboardSnapshot(
    int TotalUnitsInStock,
    int ActiveOrders,
    int LowStockCount,
    decimal MonthRevenue,
    IReadOnlyList<DashboardProductRow> Products);