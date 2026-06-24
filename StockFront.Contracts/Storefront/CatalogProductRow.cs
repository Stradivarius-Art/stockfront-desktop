namespace StockFront.Contracts.Storefront;

/// <summary>
/// One product card on the storefront catalog: what to show (name, category, price) and whether it can
/// be bought (<see cref="Availability"/>) and up to how many (<see cref="Available"/>, the unreserved
/// quantity that caps a cart line). Projected from the warehouse stock by <c>IStorefrontService</c>.
/// </summary>
public sealed record CatalogProductRow(
    int Id,
    string Sku,
    string Name,
    string Category,
    decimal Price,
    int Available,
    StorefrontAvailability Availability,
    string? Description = null,
    string? ImagePath = null);