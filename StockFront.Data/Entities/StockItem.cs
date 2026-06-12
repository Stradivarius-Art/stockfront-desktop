namespace StockFront.Data.Entities;

/// <summary>Warehouse stock for a product: quantity on hand and the amount reserved by orders.</summary>
public sealed class StockItem
{
    public int Id { get; set; }

    public int ProductId { get; set; }
    public Product Product { get; set; } = null!;

    /// <summary>Total quantity physically on hand in the warehouse.</summary>
    public int Quantity { get; set; }

    /// <summary>Quantity reserved by orders but not yet shipped.</summary>
    public int Reserved { get; set; }

    /// <summary>Quantity available for new orders. Computed, not stored.</summary>
    public int Available => Quantity - Reserved;

    /// <summary>
    /// Optimistic-locking concurrency token. MySQL updates it on every change to the row
    /// (timestamp(6) DEFAULT CURRENT_TIMESTAMP(6) ON UPDATE CURRENT_TIMESTAMP(6)), so two
    /// simultaneous reservations of the last item cannot both succeed.
    /// </summary>
    public DateTime RowVersion { get; set; }
}
