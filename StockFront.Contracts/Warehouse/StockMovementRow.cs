namespace StockFront.Contracts.Warehouse;

/// <summary>
/// One entry of the stock-movement journal for the operations history UI. <see cref="Quantity"/> is
/// signed (+ receipt, − write-off; a reversal carries the opposite sign of the entry it undoes).
/// <see cref="CanRevert"/> already accounts for the current user's rights and whether the entry is
/// itself a reversal or has already been reverted, so the UI can bind it straight to the button.
/// </summary>
public sealed record StockMovementRow(
    int Id,
    int ProductId,
    string ProductName,
    string TypeLabel,
    int Quantity,
    string? Reason,
    string? Comment,
    string? PerformedBy,
    DateTime CreatedAt,
    int? ReversesMovementId,
    bool IsReversal,
    bool IsReversed,
    bool CanRevert);