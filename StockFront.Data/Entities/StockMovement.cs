using StockFront.Contracts.Warehouse;

namespace StockFront.Data.Entities;

/// <summary>
/// One entry in the stock movement journal: a receipt, a write-off, or a reversal of either.
/// Append-only — entries are never edited or deleted. A mistake is undone by appending a reversal
/// (see <see cref="ReversesMovementId"/>), so the journal stays a complete, auditable history and the
/// "movement of goods" report has a source.
/// </summary>
public sealed class StockMovement
{
    public int Id { get; set; }

    public int ProductId { get; set; }
    public Product Product { get; set; } = null!;

    public StockMovementType Type { get; set; }

    /// <summary>
    /// Signed number of units moved: positive for a receipt, negative for a write-off; a reversal
    /// carries the exact opposite sign of the entry it undoes. The running balance lives in
    /// <see cref="StockItem.Quantity"/> — this column is the audit trail of how it got there.
    /// </summary>
    public int Quantity { get; set; }

    /// <summary>Why the stock was written off. <c>null</c> for receipts and reversals.</summary>
    public WriteOffReason? Reason { get; set; }

    /// <summary>
    /// For a reversal, the id of the original movement being undone; <c>null</c> for ordinary entries.
    /// At most one reversal may point at a given movement.
    /// </summary>
    public int? ReversesMovementId { get; set; }

    /// <summary>Free-text note, e.g. the reason an operation was rolled back.</summary>
    public string? Comment { get; set; }

    /// <summary>Display name of the operator who performed the movement, captured at the time.</summary>
    public string? PerformedBy { get; set; }

    public DateTime CreatedAt { get; set; }
}