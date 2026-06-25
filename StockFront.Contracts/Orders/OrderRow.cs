namespace StockFront.Contracts.Orders;

/// <summary>
/// A read model for one order in the Orders list: who placed it, when, how many line items, the total
/// amount and its current <see cref="OrderStatus"/>. Built by the data layer; the host renders it.
/// </summary>
public sealed record OrderRow(
    int Id,
    string CustomerName,
    DateTime CreatedAt,
    int ItemCount,
    decimal Total,
    OrderStatus Status);