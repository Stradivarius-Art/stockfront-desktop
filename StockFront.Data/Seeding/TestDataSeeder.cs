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

        var tools = new Category { Name = "Электроинструмент" };
        var accessories = new Category { Name = "Оснастка" };

        // Mirrors the products shown in the dashboard mock-up, with sensible prices.
        var drill      = Product("DR-104", "Дрель аккумуляторная", 4990m, tools, quantity: 52);
        var screwdriver = Product("SC-220", "Шуруповёрт сетевой", 3200m, tools, quantity: 8);
        var bitSet     = Product("BT-032", "Набор бит, 32 шт.", 890m, accessories, quantity: 140);
        var hammerDrill = Product("PF-880", "Перфоратор SDS-plus", 7600m, tools, quantity: 0);
        var laserLevel = Product("LV-330", "Уровень лазерный", 5400m, tools, quantity: 24);
        var grinder    = Product("AG-125", "УШМ 125 мм", 4100m, tools, quantity: 17);
        var tape       = Product("MT-050", "Рулетка 5 м", 350m, accessories, quantity: 6);

        db.Categories.AddRange(tools, accessories);
        db.Products.AddRange(drill, screwdriver, bitSet, hammerDrill, laserLevel, grinder, tape);

        // A handful of orders across statuses; paid/completed ones this month drive "Выручка за месяц",
        // new/reserved/paid ones drive "Активных заказов". CreatedAt is set by the database default.
        db.Orders.AddRange(
            Order("Иванов И.", OrderStatus.Completed,
                Line(drill, 2), Line(bitSet, 1)),
            Order("Петров П.", OrderStatus.Paid,
                Line(laserLevel, 1)),
            Order("ООО «Стройка»", OrderStatus.New,
                Line(screwdriver, 3), Line(tape, 2)),
            Order("Сидоров А.", OrderStatus.Reserved,
                Line(grinder, 1)),
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