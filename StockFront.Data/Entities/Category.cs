namespace StockFront.Data.Entities;

/// <summary>Product category. Groups products in the catalog.</summary>
public sealed class Category
{
    public int Id { get; set; }

    public string Name { get; set; } = null!;

    /// <summary>Products that belong to this category.</summary>
    public ICollection<Product> Products { get; } = new List<Product>();
}
