namespace StockFront.Contracts.Warehouse;

/// <summary>Result of attempting to revert a stock movement, so the service can map it to a message.</summary>
public enum RevertOutcome
{
    Success,

    /// <summary>No movement with the given id.</summary>
    NotFound,

    /// <summary>The movement was already reversed by a later entry.</summary>
    AlreadyReversed,

    /// <summary>The movement is itself a reversal — reversals can't be reverted.</summary>
    CannotRevertReversal,

    /// <summary>Reverting a receipt would drop stock below what's already reserved.</summary>
    InsufficientStock
}