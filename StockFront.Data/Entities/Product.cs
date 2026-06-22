namespace StockFront.Data.Entities;

/// <summary>A product in the nomenclature. Has a unique SKU and a single warehouse stock record.</summary>
public sealed class Product
{
    public int Id { get; set; }

    /// <summary>Stock-keeping unit. Unique across the nomenclature (enforced by the database).</summary>
    public string Sku { get; set; } = null!;

    public string Name { get; set; } = null!;

    /// <summary>Sale price. Always money (decimal), never floating point.</summary>
    public decimal Price { get; set; }

    public int CategoryId { get; set; }
    public Category Category { get; set; } = null!;

    /// <summary>When the product was added to the nomenclature. Drives the "Новинка" grace period.</summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>Average supplier delivery time, in days. Part of the reorder point for "Критично".</summary>
    public int LeadTimeDays { get; set; }

    /// <summary>Safety stock expressed in days of cover, added on top of <see cref="LeadTimeDays"/>.</summary>
    public int SafetyBufferDays { get; set; }

    /// <summary>
    /// Manually flagged seasonal item. Off-season (no current sales) its overstock/dead-stock alarms
    /// are suppressed so winter tyres in summer don't read as "Избыток".
    /// </summary>
    public bool IsSeasonal { get; set; }

    /// <summary>One-to-one warehouse stock record for this product.</summary>
    public StockItem? Stock { get; set; }
}
