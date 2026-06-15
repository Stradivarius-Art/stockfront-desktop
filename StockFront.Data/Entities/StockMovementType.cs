namespace StockFront.Data.Entities;

/// <summary>Direction of a stock movement: goods coming in or being written off.</summary>
public enum StockMovementType
{
    /// <summary>Приёмка — quantity added to stock.</summary>
    Receipt,

    /// <summary>Списание — quantity removed from stock.</summary>
    WriteOff
}