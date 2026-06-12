namespace StockFront.Data.Entities;

/// <summary>A customer order placed from the storefront. Has one or more line items.</summary>
public sealed class Order
{
    public int Id { get; set; }

    public string CustomerName { get; set; } = null!;

    public OrderStatus Status { get; set; } = OrderStatus.New;

    public DateTime CreatedAt { get; set; }

    /// <summary>Line items of the order.</summary>
    public ICollection<OrderLine> Lines { get; } = new List<OrderLine>();
}
