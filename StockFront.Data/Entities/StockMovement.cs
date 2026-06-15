using StockFront.Contracts.Warehouse;

namespace StockFront.Data.Entities;

/// <summary>
/// One entry in the stock movement journal: a receipt or a write-off of a product. Append-only —
/// it records what changed, by how much, why and by whom, so stock changes are auditable and the
/// "movement of goods" report has a source.
/// </summary>
public sealed class StockMovement
{
    public int Id { get; set; }

    public int ProductId { get; set; }
    public Product Product { get; set; } = null!;

    public StockMovementType Type { get; set; }

    /// <summary>Number of units moved. Always positive; the <see cref="Type"/> gives the direction.</summary>
    public int Quantity { get; set; }

    /// <summary>Why the stock was written off. <c>null</c> for receipts.</summary>
    public WriteOffReason? Reason { get; set; }

    /// <summary>Display name of the operator who performed the movement, captured at the time.</summary>
    public string? PerformedBy { get; set; }

    public DateTime CreatedAt { get; set; }
}