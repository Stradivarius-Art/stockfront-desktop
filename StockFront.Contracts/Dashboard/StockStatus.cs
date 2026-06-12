namespace StockFront.Contracts.Dashboard;

/// <summary>
/// Availability of a product on the warehouse dashboard, by stock thresholds. Maps to the coloured
/// status badges in the UI: in stock (green), low (amber), none (red).
/// </summary>
public enum StockStatus
{
    /// <summary>В наличии — enough on hand.</summary>
    InStock,

    /// <summary>Мало — at or below the low-stock threshold, but not zero.</summary>
    Low,

    /// <summary>Нет — nothing available.</summary>
    OutOfStock
}
