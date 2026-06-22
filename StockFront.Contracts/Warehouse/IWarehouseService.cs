namespace StockFront.Contracts.Warehouse;

/// <summary>
/// The Warehouse Accounting module's contract: the stock list plus goods receipt and write-off.
/// The integration point for the rest of the system — the storefront reads availability/prices and
/// orders reserve and ship through this same service (those methods arrive with their modules).
/// All operations are asynchronous and validate their input, returning a <see cref="WarehouseResult"/>
/// rather than throwing on a business-rule violation.
/// </summary>
public interface IWarehouseService
{
    /// <summary>The full product list with stock, price and availability status, ordered by name.</summary>
    Task<IReadOnlyList<WarehouseProductRow>> GetProductsAsync(CancellationToken ct = default);

    /// <summary>Categories for the "new product" form in the receiving dialog.</summary>
    Task<IReadOnlyList<CategoryOption>> GetCategoriesAsync(CancellationToken ct = default);

    /// <summary>Receive goods for an existing product: add <paramref name="quantity"/> to its stock.</summary>
    Task<WarehouseResult> ReceiveAsync(int productId, int quantity, CancellationToken ct = default);

    /// <summary>
    /// Register a brand-new product (nomenclature) and receive its first <paramref name="quantity"/>
    /// into stock. Fails if the SKU is already taken.
    /// </summary>
    Task<WarehouseResult> ReceiveNewProductAsync(
        string sku, string name, int categoryId, decimal price, int quantity, CancellationToken ct = default);

    /// <summary>
    /// Write off <paramref name="quantity"/> of a product for the given <paramref name="reason"/>.
    /// Fails if more than the available (unreserved) quantity is requested.
    /// </summary>
    Task<WarehouseResult> WriteOffAsync(
        int productId, int quantity, WriteOffReason reason, CancellationToken ct = default);

    /// <summary>
    /// The stock-movement journal (newest first), optionally filtered to one product. Each row already
    /// knows whether the current user may revert it.
    /// </summary>
    Task<IReadOnlyList<StockMovementRow>> GetMovementsAsync(int? productId = null, CancellationToken ct = default);

    /// <summary>
    /// Revert a receipt or write-off by appending a compensating entry (the original is never changed
    /// or deleted). Admin only; fails if the entry is itself a reversal, was already reverted, or the
    /// reversal would push stock below what's reserved.
    /// </summary>
    Task<WarehouseResult> RevertMovementAsync(int movementId, CancellationToken ct = default);
}