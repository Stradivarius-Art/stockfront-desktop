namespace StockFront.Data.Entities;

/// <summary>A single line of an order: a product, its quantity, and the price at the moment of sale.</summary>
public sealed class OrderLine
{
    public int Id { get; set; }

    public int OrderId { get; set; }
    public Order Order { get; set; } = null!;

    public int ProductId { get; set; }
    public Product Product { get; set; } = null!;

    public int Quantity { get; set; }

    /// <summary>Unit price captured when the line was created. Money — decimal(12,2).</summary>
    public decimal UnitPrice { get; set; }
}
