using StockFront.Contracts.Dashboard;
using StockFront.Contracts.Storefront;
using StockFront.Contracts.Warehouse;

namespace StockFront.Storefront.Services;

/// <summary>
/// Storefront business logic. Owns no stock of its own: it reads the catalog and prices from the
/// Warehouse Accounting module (<see cref="IWarehouseService"/>) and projects each product into the
/// simpler storefront shape — price, available quantity and a three-step availability light. The
/// admin card edit is delegated straight back to the warehouse (it's nomenclature). Because all data
/// comes through the warehouse contract, this class can be unit-tested with a mocked service.
/// </summary>
public sealed class StorefrontService : IStorefrontService
{
    private readonly IWarehouseService _warehouse;

    public StorefrontService(IWarehouseService warehouse) => _warehouse = warehouse;

    public async Task<IReadOnlyList<CatalogProductRow>> GetCatalogAsync(CancellationToken ct = default)
    {
        var products = await _warehouse.GetProductsAsync(ct);
        return products
            .Select(p => new CatalogProductRow(
                p.Id, p.Sku, p.Name, p.Category, p.Price, p.Available, ToAvailability(p),
                p.Description, p.ImagePath))
            .ToList();
    }

    public Task<IReadOnlyList<CategoryOption>> GetCategoriesAsync(CancellationToken ct = default) =>
        _warehouse.GetCategoriesAsync(ct);

    public Task<WarehouseResult> UpdateProductPresentationAsync(
        int productId, string? imagePath, string? description, CancellationToken ct = default) =>
        _warehouse.UpdateProductPresentationAsync(productId, imagePath, description, ct);

    /// <summary>
    /// Collapse the warehouse's nine-status traffic light into the storefront's three steps: nothing to
    /// sell (all reserved or empty) reads as out of stock, the warehouse's "критично"/"мало" map to
    /// "мало", everything else is freely orderable.
    /// </summary>
    private static StorefrontAvailability ToAvailability(WarehouseProductRow p) =>
        p.Available <= 0 ? StorefrontAvailability.OutOfStock
        : p.Status is StockStatus.Critical or StockStatus.Low ? StorefrontAvailability.Low
        : StorefrontAvailability.InStock;
}