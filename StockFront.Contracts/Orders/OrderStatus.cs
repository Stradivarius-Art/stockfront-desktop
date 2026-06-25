namespace StockFront.Contracts.Orders;

/// <summary>
/// Order lifecycle states (see diagrams/diagram_state.png):
/// New → Reserved → Paid → Shipped → Completed, with Cancelled as a terminal branch.
/// Owned by the Orders contract; stored in the database as a string for readability.
/// </summary>
public enum OrderStatus
{
    /// <summary>Новая — created from the storefront; stock is already reserved (held until shipment).</summary>
    New,

    /// <summary>Зарезервирована — stock reserved in the warehouse.</summary>
    Reserved,

    /// <summary>Оплачена — paid by the customer.</summary>
    Paid,

    /// <summary>Отгружена — shipped, stock written off.</summary>
    Shipped,

    /// <summary>Завершена — fulfilled and closed.</summary>
    Completed,

    /// <summary>Отменена — cancelled, any reservation released.</summary>
    Cancelled
}