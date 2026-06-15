using Microsoft.EntityFrameworkCore;
using StockFront.Data.Entities;

namespace StockFront.Data.Seeding;

/// <summary>
/// Fills the database with a small, illustrative dataset (categories, products with stock, and a
/// few orders) so the dashboard isn't empty during development. Run on demand via the terminal
/// command <c>-- --seed</c> (never on startup). Idempotent: it does nothing once any product
/// exists, so it's safe to run repeatedly. Not for production data.
/// </summary>
public static class TestDataSeeder
{
    public static async Task SeedAsync(AppDbContext db, CancellationToken ct = default)
    {
        if (await db.Products.AnyAsync(ct))
            return;

        var tools = new Category { Name = "Инструмент" };
        var consumables = new Category { Name = "Расходники" };
        var measuring = new Category { Name = "Измерительный" };
        var fasteners = new Category { Name = "Крепёж" };

        // Mirrors the products shown in the warehouse mock-up (design/stockfront_sklad.png).
        var drill       = Product("DR-104", "Дрель аккумуляторная", 4990m, tools, quantity: 52);
        var screwdriver = Product("SC-220", "Шуруповёрт сетевой", 3200m, tools, quantity: 8);
        var bitSet      = Product("BT-032", "Набор бит, 32 шт.", 890m, consumables, quantity: 140);
        var hammerDrill = Product("PF-880", "Перфоратор SDS-plus", 8750m, tools, quantity: 0);
        var laserLevel  = Product("LV-330", "Уровень лазерный", 6400m, measuring, quantity: 24);
        var cutDisc     = Product("DC-125", "Диск отрезной, 125 мм", 75m, consumables, quantity: 320);
        var screw       = Product("SR-440", "Шуруп 4×40, упаковка", 210m, fasteners, quantity: 12);
        var square      = Product("SQ-200", "Угольник столярный", 540m, measuring, quantity: 36);

        db.Categories.AddRange(tools, consumables, measuring, fasteners);
        db.Products.AddRange(drill, screwdriver, bitSet, hammerDrill, laserLevel, cutDisc, screw, square);

        // A handful of orders across statuses; paid/completed ones this month drive "Выручка за месяц",
        // new/reserved/paid ones drive "Активных заказов". CreatedAt is set by the database default.
        db.Orders.AddRange(
            Order("Иванов И.", OrderStatus.Completed,
                Line(drill, 2), Line(bitSet, 1)),
            Order("Петров П.", OrderStatus.Paid,
                Line(laserLevel, 1)),
            Order("ООО «Стройка»", OrderStatus.New,
                Line(screwdriver, 3), Line(cutDisc, 10)),
            Order("Сидоров А.", OrderStatus.Reserved,
                Line(square, 1)),
            Order("Кузнецов Д.", OrderStatus.Shipped,
                Line(drill, 1)));

        await db.SaveChangesAsync(ct);
    }

    private static Product Product(string sku, string name, decimal price, Category category, int quantity) =>
        new()
        {
            Sku = sku,
            Name = name,
            Price = price,
            Category = category,
            Stock = new StockItem { Quantity = quantity, Reserved = 0 }
        };

    private static Order Order(string customer, OrderStatus status, params OrderLine[] lines)
    {
        var order = new Order { CustomerName = customer, Status = status };
        foreach (var line in lines)
            order.Lines.Add(line);
        return order;
    }

    private static OrderLine Line(Product product, int quantity) =>
        new() { Product = product, Quantity = quantity, UnitPrice = product.Price };
}