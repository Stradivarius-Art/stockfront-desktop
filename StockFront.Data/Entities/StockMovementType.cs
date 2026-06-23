namespace StockFront.Data.Entities;

/// <summary>Direction/kind of a stock movement: goods coming in, written off, or a reversal of either.</summary>
public enum StockMovementType
{
    /// <summary>Приёмка — quantity added to stock (positive quantity).</summary>
    Receipt,

    /// <summary>Списание — quantity removed from stock (negative quantity).</summary>
    WriteOff,

    /// <summary>Продажа — quantity shipped to a customer order (negative quantity).</summary>
    Sale,

    /// <summary>Откат приёмки — compensating entry that undoes a receipt (negative quantity).</summary>
    ReversalReceipt,

    /// <summary>Откат списания — compensating entry that undoes a write-off (positive quantity).</summary>
    ReversalWriteOff
}