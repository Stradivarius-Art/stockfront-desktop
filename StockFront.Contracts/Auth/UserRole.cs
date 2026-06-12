namespace StockFront.Contracts.Auth;

/// <summary>
/// Access level of an account. Roles gate which windows and commands are available
/// (enforced through <see cref="ICurrentUser"/>, not just hidden in the UI).
/// Stored in the database as an <c>int</c>; the explicit values keep that mapping stable.
/// </summary>
public enum UserRole
{
    /// <summary>Покупатель — sees only the storefront: catalog, cart, their own orders.</summary>
    Customer = 0,

    /// <summary>Кладовщик — manages the warehouse and orders, but not user administration.</summary>
    WarehouseKeeper = 1,

    /// <summary>Администратор — full access, including managing users.</summary>
    Admin = 2
}
