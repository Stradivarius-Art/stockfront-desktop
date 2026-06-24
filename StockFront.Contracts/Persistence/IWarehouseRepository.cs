using StockFront.Contracts.Warehouse;

namespace StockFront.Contracts.Persistence;

/// <summary>
/// Data-layer access for the warehouse module. Pure persistence — no business rules: the
/// <see cref="IWarehouseService"/> validates input and classifies stock, this only reads and writes.
/// Stock-changing operations also append to the movement journal so receipts and write-offs are
/// auditable.
/// </summary>
public interface IWarehouseRepository
{
    /// <summary>All products with their raw stock figures, ordered by name.</summary>
    Task<IReadOnlyList<ProductStockRecord>> GetProductsAsync(CancellationToken ct = default);

    /// <summary>All categories, ordered by name.</summary>
    Task<IReadOnlyList<CategoryOption>> GetCategoriesAsync(CancellationToken ct = default);

    /// <summary>True if a product with this SKU already exists.</summary>
    Task<bool> SkuExistsAsync(string sku, CancellationToken ct = default);

    /// <summary>Add <paramref name="quantity"/> to a product's stock and journal the receipt.</summary>
    Task ReceiveAsync(int productId, int quantity, string? performedBy, CancellationToken ct = default);

    /// <summary>
    /// Update a product's name, price and category. Returns <c>false</c> if the product was not found.
    /// </summary>
    Task<bool> UpdateProductDetailsAsync(
        int productId, string name, decimal price, int categoryId, CancellationToken ct = default);

    /// <summary>
    /// Update a product's storefront presentation (image path and description). Returns <c>false</c> if
    /// the product was not found.
    /// </summary>
    Task<bool> UpdateProductPresentationAsync(
        int productId, string? imagePath, string? description, CancellationToken ct = default);

    /// <summary>
    /// Create a product together with its stock record (the first received quantity) and journal the
    /// receipt. Returns the new product id.
    /// </summary>
    Task<int> CreateProductWithStockAsync(
        string sku, string name, int categoryId, decimal price, int quantity,
        string? performedBy, CancellationToken ct = default);

    /// <summary>
    /// Subtract <paramref name="quantity"/> from a product's stock and journal the write-off. Returns
    /// <c>false</c> without changing anything if more than the available quantity was requested.
    /// </summary>
    Task<bool> WriteOffAsync(
        int productId, int quantity, WriteOffReason reason, string? performedBy, CancellationToken ct = default);

    /// <summary>
    /// Delete an empty product together with its stock record and movement journal, in one transaction.
    /// Refuses (without changes) if the product still has stock or is referenced by order lines. The
    /// outcome says whether — and why not — it happened.
    /// </summary>
    Task<DeleteProductOutcome> DeleteProductAsync(int productId, CancellationToken ct = default);

    /// <summary>The stock-movement journal (newest first), optionally filtered to one product.</summary>
    Task<IReadOnlyList<StockMovementRow>> GetMovementsAsync(int? productId, CancellationToken ct = default);

    /// <summary>
    /// Append a compensating entry that undoes movement <paramref name="movementId"/> and adjust the
    /// stock, in a single transaction. Never edits or deletes the original. The return value reports
    /// whether (and why not) the reversal happened.
    /// </summary>
    Task<RevertOutcome> RevertMovementAsync(int movementId, string? performedBy, CancellationToken ct = default);
}