using StockFront.Contracts.Warehouse;

namespace StockFront.Contracts.Storefront;

/// <summary>
/// The Storefront module's contract: the public catalog of orderable goods with prices and
/// availability, plus the admin-only product card edit. Reads stock and prices from the Warehouse
/// Accounting module (<see cref="IWarehouseService"/>) — the storefront owns no stock of its own, it
/// only presents what the warehouse holds (see CLAUDE.md "Integration points").
/// </summary>
public interface IStorefrontService
{
    /// <summary>The catalog: every product with its price and storefront availability, ordered by name.</summary>
    Task<IReadOnlyList<CatalogProductRow>> GetCatalogAsync(CancellationToken ct = default);

    /// <summary>Categories for the storefront filter chips (Все + one per category).</summary>
    Task<IReadOnlyList<CategoryOption>> GetCategoriesAsync(CancellationToken ct = default);

    /// <summary>
    /// Admin-only: edit a product card's presentation — its image and description. Stock, name and price
    /// are not touched (those belong to the warehouse). The change is persisted through the warehouse.
    /// </summary>
    Task<WarehouseResult> UpdateProductPresentationAsync(
        int productId, string? imagePath, string? description, CancellationToken ct = default);
}