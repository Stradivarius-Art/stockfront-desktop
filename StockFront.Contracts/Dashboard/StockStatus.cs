namespace StockFront.Contracts.Dashboard;

/// <summary>
/// Availability of a product, derived from its Days-of-Supply (stock ÷ average daily sales) rather
/// than a fixed unit threshold. Computed on read, never stored — so it can't go stale. Maps to the
/// coloured status badges in the UI. Order of severity is encoded by <see cref="StockStatusCalculator"/>,
/// not by the enum's numeric values (the status is not persisted).
/// </summary>
public enum StockStatus
{
    /// <summary>Нет — nothing on hand (<c>qty == 0</c>). Red.</summary>
    OutOfStock,

    /// <summary>Критично — DoS below the reorder point (lead time + safety buffer). Orange.</summary>
    Critical,

    /// <summary>Мало — DoS below two weeks (or slow-moving with no recent sales). Amber.</summary>
    Low,

    /// <summary>Норма — DoS within the healthy band (14…60 days). Green.</summary>
    Normal,

    /// <summary>Выше нормы — DoS in the border zone (60…90 days). Teal.</summary>
    AboveNormal,

    /// <summary>Избыток — DoS over 90 days, capital frozen in stock. Blue.</summary>
    Overstock,

    /// <summary>Новинка — recently added, no sales yet; DoS rules don't apply. Purple.</summary>
    New,

    /// <summary>Неликвид — on hand but no sale in a long time (dead stock). Grey.</summary>
    Illiquid,

    /// <summary>Сезон — seasonal item out of season; overstock/dead-stock alarms suppressed. Neutral.</summary>
    Seasonal
}