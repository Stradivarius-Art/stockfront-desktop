namespace StockFront.Contracts.Persistence;

/// <summary>
/// Raw stock figures for one product as read from the data layer. The availability status is not
/// here — the warehouse module derives it from these numbers (the low-stock threshold is a business
/// rule, not a storage concern).
/// </summary>
public sealed record ProductStockRecord(
    int Id,
    string Sku,
    string Name,
    string Category,
    int Quantity,
    int Reserved,
    decimal Price);