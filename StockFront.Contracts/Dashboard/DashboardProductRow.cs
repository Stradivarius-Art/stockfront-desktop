namespace StockFront.Contracts.Dashboard;

/// <summary>One row of the dashboard products table: name, SKU, stock on hand and its status.</summary>
public sealed record DashboardProductRow(string Name, string Sku, int Stock, StockStatus Status);