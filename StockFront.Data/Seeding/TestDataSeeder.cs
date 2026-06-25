using Microsoft.EntityFrameworkCore;
using StockFront.Contracts.Orders;
using StockFront.Data.Entities;

namespace StockFront.Data.Seeding;

/// <summary>
/// Fills the database with a small, illustrative dataset (categories, products with stock, and a few
/// orders) so the dashboard isn't empty during development. The products and sales are chosen so every
/// Days-of-Supply status shows up at least once (нет / критично / мало / норма / выше нормы / избыток /
/// новинка / неликвид / сезон). Run on demand via the terminal command <c>-- --seed</c> (never on
/// startup). Idempotent: it does nothing once any product exists. Not for production data.
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
        var seasonal = new Category { Name = "Сезонное" };

        // Stock + lead/safety are tuned together with the sales below to land on a specific status.
        var drill = Product("DR-104", "Дрель аккумуляторная", 4990m, tools, quantity: 52,   // норма
            description: "Аккумуляторная дрель-шуруповёрт 18В с двумя скоростями и реверсом. В комплекте два аккумулятора и зарядное устройство.");
        var screwdriver = Product("SC-220", "Шуруповёрт сетевой", 3200m, tools, quantity: 8,       // критично
            description: "Сетевой шуруповёрт мощностью 600 Вт с регулировкой крутящего момента. Подходит для длительной работы без подзарядки.");
        var bitSet = Product("BT-032", "Набор бит, 32 шт.", 890m, consumables, quantity: 140, // избыток
            description: "Набор из 32 бит из хром-ванадиевой стали: шлицевые, крестовые, шестигранные и Torx. Удобный пластиковый кейс.");
        var hammerDrill = Product("PF-880", "Перфоратор SDS-plus", 8750m, tools, quantity: 0,      // нет
            description: "Перфоратор SDS-plus 900 Вт с тремя режимами работы: сверление, сверление с ударом и долбление.");
        var laserLevel = Product("LV-330", "Уровень лазерный", 6400m, measuring, quantity: 24,    // мало
            description: "Самовыравнивающийся лазерный уровень с проекцией горизонтальной и вертикальной линий. Дальность до 20 м.");
        var cutDisc = Product("DC-125", "Диск отрезной, 125 мм", 75m, consumables, quantity: 320, // избыток
            description: "Отрезной диск по металлу 125×1,2 мм для углошлифовальных машин. Толщина обеспечивает чистый и быстрый рез.");
        var screw = Product("SR-440", "Шуруп 4×40, упаковка", 210m, fasteners, quantity: 36, // норма
            description: "Универсальные саморезы 4×40 мм с потайной головкой, оцинкованные. Упаковка 200 шт.");
        var square = Product("SQ-200", "Угольник столярный", 540m, measuring, quantity: 36,   // выше нормы
            description: "Столярный угольник 200 мм из нержавеющей стали с алюминиевым основанием. Точная разметка под 90° и 45°.");

        // Edge cases: a brand-new item (no sales yet), dead stock (old, never sold), seasonal off-season.
        var newDriver = Product("NW-500", "Гайковёрт ударный (новинка)", 7200m, tools, quantity: 6,     // новинка
            description: "Аккумуляторный ударный гайковёрт с моментом затяжки до 400 Нм. Бесщёточный двигатель для повышенного ресурса.");
        var deadStock = Product("OLD-900", "Тиски слесарные", 2300m, tools, quantity: 15,              // неликвид
            description: "Слесарные тиски с шириной губок 100 мм и поворотным основанием. Чугунный корпус для надёжной фиксации заготовок.");
        var heater = Product("SE-700", "Тепловентилятор", 3100m, seasonal, quantity: 40, isSeasonal: true, // сезон
            description: "Тепловентилятор 2000 Вт с регулировкой мощности и режимом вентиляции. Защита от перегрева.");

        db.Categories.AddRange(tools, consumables, measuring, fasteners, seasonal);
        db.Products.AddRange(
            drill, screwdriver, bitSet, hammerDrill, laserLevel, cutDisc, screw, square,
            newDriver, deadStock, heater);

        // Active orders (drive "Активных заказов" / "Выручка за месяц" on the dashboard).
        db.Orders.AddRange(
            Order("Петров П.", OrderStatus.Paid, Line(laserLevel, 1)),
            Order("ООО «Стройка»", OrderStatus.New, Line(screwdriver, 3), Line(cutDisc, 10)),
            Order("Сидоров А.", OrderStatus.Reserved, Line(square, 1)));

        // Sales history (shipped/completed) within the 30-day window — this is the avg_daily_sales the
        // status is derived from. Totals per product are chosen to hit the target Days-of-Supply band.
        db.Orders.Add(Order("История продаж", OrderStatus.Completed,
            Line(drill, 39),        // 52 / (39/30)=1.3 ≈ 40 дн. → норма
            Line(screwdriver, 30),  // 8  / 1.0 = 8 дн.  → критично
            Line(bitSet, 30),       // 140 / 1.0 = 140 дн. → избыток
            Line(laserLevel, 60),   // 24 / 2.0 = 12 дн. → мало
            Line(cutDisc, 90),      // 320 / 3.0 ≈ 107 дн. → избыток
            Line(screw, 27),        // 36 / 0.9 = 40 дн. → норма
            Line(square, 15)));     // 36 / 0.5 = 72 дн. → выше нормы

        await db.SaveChangesAsync(ct);

        // Back-date the dead-stock item so it isn't treated as "новинка" (no sales + older than 30 days
        // → неликвид). The created_at column is database-generated on insert, so we set it afterwards.
        deadStock.CreatedAt = DateTime.UtcNow.AddDays(-200);
        db.Entry(deadStock).Property(p => p.CreatedAt).IsModified = true;
        await db.SaveChangesAsync(ct);
    }

    private static Product Product(
        string sku, string name, decimal price, Category category, int quantity,
        int leadTimeDays = 7, int safetyBufferDays = 3, bool isSeasonal = false,
        string? description = null) =>
        new()
        {
            Sku = sku,
            Name = name,
            Price = price,
            Category = category,
            Description = description,
            LeadTimeDays = leadTimeDays,
            SafetyBufferDays = safetyBufferDays,
            IsSeasonal = isSeasonal,
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