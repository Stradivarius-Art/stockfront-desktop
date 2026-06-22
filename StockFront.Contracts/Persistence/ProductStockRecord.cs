namespace StockFront.Contracts.Persistence;

/// <summary>
/// Raw stock figures for one product as read from the data layer, plus the demand/recency signals the
/// warehouse module needs to derive availability (Days of Supply). The status itself is not here —
/// the warehouse module computes it from these numbers (the thresholds are a business rule, not a
/// storage concern).
/// </summary>
public sealed record ProductStockRecord(
    int Id,
    string Sku,
    string Name,
    string Category,
    int Quantity,
    int Reserved,
    decimal Price,
    DateTime CreatedAt,
    int LeadTimeDays,
    int SafetyBufferDays,
    bool IsSeasonal,
    double AvgDailySales,
    DateTime? LastSale);