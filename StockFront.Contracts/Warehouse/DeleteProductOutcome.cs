namespace StockFront.Contracts.Warehouse;

/// <summary>Result of attempting to delete a product, so the service can map it to a message.</summary>
public enum DeleteProductOutcome
{
    Success,

    /// <summary>No product with the given id.</summary>
    NotFound,

    /// <summary>The product still has stock on hand (or reserved) — only empty products may be deleted.</summary>
    HasStock,

    /// <summary>The product is referenced by existing orders, so its history must be kept.</summary>
    InUse
}