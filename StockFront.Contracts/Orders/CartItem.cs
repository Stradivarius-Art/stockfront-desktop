namespace StockFront.Contracts.Orders;

/// <summary>A single requested line when placing an order: which product and how many units.</summary>
public sealed record CartItem(int ProductId, int Quantity);