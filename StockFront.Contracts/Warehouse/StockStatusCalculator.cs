using StockFront.Contracts.Dashboard;

namespace StockFront.Contracts.Warehouse;

/// <summary>
/// Everything needed to classify one product's availability. A pure value with no dependency on the
/// data layer, so the rule can be unit-tested and shared by both the warehouse module and the
/// dashboard read model. <paramref name="Now"/> is passed in (not read from the clock) to keep the
/// calculation deterministic and testable.
/// </summary>
public readonly record struct StockStatusInputs(
    int Quantity,
    double AvgDailySales,
    DateTime? LastSale,
    DateTime CreatedAt,
    int LeadTimeDays,
    int SafetyBufferDays,
    bool IsSeasonal,
    DateTime Now);

/// <summary>
/// Derives a product's <see cref="StockStatus"/> from its stock and recent demand using the
/// Days-of-Supply method (<c>DoS = qty / avg_daily_sales</c>). The single source of truth for the
/// availability traffic light. Rules are checked in order of precedence — the first match wins — and
/// the zero-sales branches (New / Illiquid / Seasonal) are resolved <i>before</i> any division, so a
/// product with no sales can never trigger a divide-by-zero.
/// </summary>
public static class StockStatusCalculator
{
    /// <summary>A product newer than this with no sales yet is treated as "Новинка", not judged by DoS.</summary>
    public const int NewProductDays = 30;

    /// <summary>No sale within this many days (with stock on hand) makes a product "Неликвид".</summary>
    public const int IlliquidDays = 90;

    /// <summary>Below this DoS (and above the reorder point) a product is "Мало".</summary>
    public const int LowUpperDays = 14;

    /// <summary>Upper bound of the healthy "Норма" band, in days of supply.</summary>
    public const int NormalUpperDays = 60;

    /// <summary>Upper bound of the "Выше нормы" border zone; beyond it is "Избыток".</summary>
    public const int AboveNormalUpperDays = 90;

    public static StockStatus Classify(in StockStatusInputs x)
    {
        if (x.Quantity <= 0)
            return StockStatus.OutOfStock;

        // Seasonal item with no current demand: don't cry "overstock/dead" out of season.
        if (x.IsSeasonal && x.AvgDailySales <= 0)
            return StockStatus.Seasonal;

        // No sales history at all: a recent arrival gets a grace period; an old one is dead stock.
        if (x.LastSale is null)
            return x.CreatedAt > x.Now.AddDays(-NewProductDays)
                ? StockStatus.New
                : StockStatus.Illiquid;

        // Sold before, but not for a long time → dead stock regardless of how much is on hand.
        if (x.LastSale < x.Now.AddDays(-IlliquidDays))
            return StockStatus.Illiquid;

        // Sold within the dead-stock window but nothing in the averaging window: slow mover.
        if (x.AvgDailySales <= 0)
            return StockStatus.Low;

        var daysOfSupply = x.Quantity / x.AvgDailySales;
        var reorderPoint = x.LeadTimeDays + x.SafetyBufferDays;

        if (daysOfSupply < reorderPoint) return StockStatus.Critical;
        if (daysOfSupply < LowUpperDays) return StockStatus.Low;
        if (daysOfSupply <= NormalUpperDays) return StockStatus.Normal;
        if (daysOfSupply <= AboveNormalUpperDays) return StockStatus.AboveNormal;
        return StockStatus.Overstock;
    }

    /// <summary>Days of supply, or <c>null</c> when there's no demand to divide by.</summary>
    public static double? DaysOfSupply(int quantity, double avgDailySales) =>
        avgDailySales > 0 ? quantity / avgDailySales : null;
}