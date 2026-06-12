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

    /// <summary>One-to-one warehouse stock record for this product.</summary>
    public StockItem? Stock { get; set; }
}
