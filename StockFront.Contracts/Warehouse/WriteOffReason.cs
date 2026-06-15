namespace StockFront.Contracts.Warehouse;

/// <summary>Why stock was written off. Shown as a choice in the write-off dialog and kept on the
/// movement journal entry so the "movement of goods" report can explain each decrease.</summary>
public enum WriteOffReason
{
    /// <summary>Брак — defective goods.</summary>
    Defective,

    /// <summary>Недостача — missing on an inventory count.</summary>
    Shortage,

    /// <summary>Порча — damaged or spoiled.</summary>
    Damage,

    /// <summary>Прочее — any other reason.</summary>
    Other
}