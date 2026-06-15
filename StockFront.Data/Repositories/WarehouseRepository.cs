using Microsoft.EntityFrameworkCore;
using StockFront.Contracts.Persistence;
using StockFront.Contracts.Warehouse;
using StockFront.Data.Entities;

namespace StockFront.Data.Repositories;

/// <summary>
/// EF Core implementation of <see cref="IWarehouseRepository"/>. Reads map entities to the
/// <c>Contracts</c> DTOs; writes change stock and append a <see cref="StockMovement"/> in the same
/// <c>SaveChanges</c>, so a receipt or write-off and its journal entry succeed or fail together.
/// </summary>
public sealed class WarehouseRepository : IWarehouseRepository
{
    private readonly AppDbContext _db;

    public WarehouseRepository(AppDbContext db) => _db = db;

    public async Task<IReadOnlyList<ProductStockRecord>> GetProductsAsync(CancellationToken ct = default)
    {
        return await _db.Products.AsNoTracking()
            .OrderBy(p => p.Name)
            .Select(p => new ProductStockRecord(
                p.Id,
                p.Sku,
                p.Name,
                p.Category.Name,
                p.Stock != null ? p.Stock.Quantity : 0,
                p.Stock != null ? p.Stock.Reserved : 0,
                p.Price))
            .ToListAsync(ct);
    }

    public async Task<IReadOnlyList<CategoryOption>> GetCategoriesAsync(CancellationToken ct = default)
    {
        return await _db.Categories.AsNoTracking()
            .OrderBy(c => c.Name)
            .Select(c => new CategoryOption(c.Id, c.Name))
            .ToListAsync(ct);
    }

    public Task<bool> SkuExistsAsync(string sku, CancellationToken ct = default) =>
        _db.Products.AsNoTracking().AnyAsync(p => p.Sku == sku, ct);

    public async Task ReceiveAsync(int productId, int quantity, string? performedBy, CancellationToken ct = default)
    {
        var stock = await _db.StockItems.FirstOrDefaultAsync(s => s.ProductId == productId, ct)
                    ?? throw new InvalidOperationException($"Не найден остаток для товара #{productId}.");

        stock.Quantity += quantity;
        AddMovement(productId, StockMovementType.Receipt, quantity, reason: null, performedBy);

        await _db.SaveChangesAsync(ct);
    }

    public async Task<int> CreateProductWithStockAsync(
        string sku, string name, int categoryId, decimal price, int quantity,
        string? performedBy, CancellationToken ct = default)
    {
        var product = new Product
        {
            Sku = sku,
            Name = name,
            CategoryId = categoryId,
            Price = price,
            Stock = new StockItem { Quantity = quantity, Reserved = 0 }
        };

        _db.Products.Add(product);
        // Save first so the movement can reference the generated product id.
        await _db.SaveChangesAsync(ct);

        AddMovement(product.Id, StockMovementType.Receipt, quantity, reason: null, performedBy);
        await _db.SaveChangesAsync(ct);

        return product.Id;
    }

    public async Task<bool> WriteOffAsync(
        int productId, int quantity, WriteOffReason reason, string? performedBy, CancellationToken ct = default)
    {
        var stock = await _db.StockItems.FirstOrDefaultAsync(s => s.ProductId == productId, ct)
                    ?? throw new InvalidOperationException($"Не найден остаток для товара #{productId}.");

        // Reserved units are committed to orders, so only the available quantity may be written off.
        if (quantity > stock.Quantity - stock.Reserved)
            return false;

        stock.Quantity -= quantity;
        AddMovement(productId, StockMovementType.WriteOff, quantity, reason, performedBy);

        await _db.SaveChangesAsync(ct);
        return true;
    }

    private void AddMovement(
        int productId, StockMovementType type, int quantity, WriteOffReason? reason, string? performedBy) =>
        _db.StockMovements.Add(new StockMovement
        {
            ProductId = productId,
            Type = type,
            Quantity = quantity,
            Reason = reason,
            PerformedBy = performedBy
        });
}