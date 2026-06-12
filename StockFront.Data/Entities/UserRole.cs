namespace StockFront.Data.Entities;

/// <summary>
/// Who a user is in the system. Drives what the WPF host shows after login:
/// the warehouse dashboard for an operator, the storefront for a customer.
/// Stored in the database as a string for readable rows (like <see cref="OrderStatus"/>).
/// </summary>
public enum UserRole
{
    /// <summary>Кладовщик — manages stock, records receipts and write-offs.</summary>
    Operator,

    /// <summary>Покупатель — browses the storefront catalog and places orders.</summary>
    Customer
}
