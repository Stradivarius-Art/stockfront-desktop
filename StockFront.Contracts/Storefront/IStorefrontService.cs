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
    /// Admin-only: edit a product card's name and price (the storefront-facing nomenclature). Stock is
    /// not touched. Validation lives here; the change is persisted through the warehouse repository.
    /// </summary>
    Task<WarehouseResult> UpdateProductCardAsync(
        int productId, string name, decimal price, CancellationToken ct = default);
}