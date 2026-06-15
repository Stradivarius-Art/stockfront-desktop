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
}