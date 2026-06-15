using StockFront.Contracts.Dashboard;

namespace StockFront.Contracts.Warehouse;

/// <summary>
/// One row of the warehouse table: identity, category, on-hand stock, price and the availability
/// status badge. <see cref="StockStatus"/> is reused from the dashboard — the same green/amber/red
/// traffic light.
/// </summary>
public sealed record WarehouseProductRow(
    int Id,
    string Sku,
    string Name,
    string Category,
    int Quantity,
    int Reserved,
    int Available,
    decimal Price,
    StockStatus Status);
