using StockFront.Contracts.Dashboard;

namespace StockFront.Contracts.Warehouse;

/// <summary>
/// One row of the warehouse table: identity, category, on-hand stock, price and the derived
/// availability. <see cref="DaysOfSupply"/> and <see cref="AvgDailySales"/> back the status badge and
/// its tooltip; <see cref="DaysOfSupply"/> is <c>null</c> when there's no recent demand to divide by.
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
    StockStatus Status,
    double AvgDailySales,
    double? DaysOfSupply,
    string? Description = null,
    string? ImagePath = null);