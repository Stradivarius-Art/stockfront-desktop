namespace StockFront.Contracts.Storefront;

/// <summary>
/// How a product reads to a shopper on the storefront card: the simple three-step traffic light from
/// the design (в наличии / мало / нет в наличии). Derived from the warehouse's richer
/// <see cref="StockFront.Contracts.Dashboard.StockStatus"/> plus the available (unreserved) quantity —
/// the storefront only cares whether you can buy it and roughly how much is left.
/// </summary>
public enum StorefrontAvailability
{
    /// <summary>В наличии — freely orderable. Green.</summary>
    InStock,

    /// <summary>Мало — running low, still orderable. Amber.</summary>
    Low,

    /// <summary>Нет в наличии — nothing available to order; the "В корзину" button is disabled. Red.</summary>
    OutOfStock
}