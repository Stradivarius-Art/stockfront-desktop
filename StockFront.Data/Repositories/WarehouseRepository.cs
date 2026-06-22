using Microsoft.EntityFrameworkCore;
using StockFront.Contracts.Persistence;
using StockFront.Contracts.Warehouse;
using StockFront.Data.Entities;

namespace StockFront.Data.Repositories;

/// <summary>
/// EF Core implementation of <see cref="IWarehouseRepository"/>. Reads map entities to the
/// <c>Contracts</c> DTOs (including the demand signals the status needs); writes change stock and
/// append a signed <see cref="StockMovement"/> in the same <c>SaveChanges</c>, so a receipt or
/// write-off and its journal entry succeed or fail together. A mistake is undone by appending a
/// reversal — entries are never edited or deleted.
/// </summary>
public sealed class WarehouseRepository : IWarehouseRepository
{
    private readonly AppDbContext _db;

    public WarehouseRepository(AppDbContext db) => _db = db;

    public async Task<IReadOnlyList<ProductStockRecord>> GetProductsAsync(CancellationToken ct = default)
    {
        var now = DateTime.UtcNow;
        var demand = await SalesDemand.LoadAsync(_db, now, ct);

        var products = await _db.Products.AsNoTracking()
            .OrderBy(p => p.Name)
            .Select(p => new
            {
                p.Id,
                p.Sku,
                p.Name,
                Category = p.Category.Name,
                Quantity = p.Stock != null ? p.Stock.Quantity : 0,
                Reserved = p.Stock != null ? p.Stock.Reserved : 0,
                p.Price,
                p.CreatedAt,
                p.LeadTimeDays,
                p.SafetyBufferDays,
                p.IsSeasonal
            })
            .ToListAsync(ct);

        return products
            .Select(p => new ProductStockRecord(
                p.Id, p.Sku, p.Name, p.Category, p.Quantity, p.Reserved, p.Price,
                p.CreatedAt, p.LeadTimeDays, p.SafetyBufferDays, p.IsSeasonal,
                AvgDailySales: demand.AvgDailySales(p.Id),
                LastSale: demand.LastSale(p.Id)))
            .ToList();
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
        AddMovement(productId, StockMovementType.Receipt, +quantity, reason: null, performedBy);

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

        AddMovement(product.Id, StockMovementType.Receipt, +quantity, reason: null, performedBy);
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
        AddMovement(productId, StockMovementType.WriteOff, -quantity, reason, performedBy);

        await _db.SaveChangesAsync(ct);
        return true;
    }

    public async Task<IReadOnlyList<StockMovementRow>> GetMovementsAsync(int? productId, CancellationToken ct = default)
    {
        var query = _db.StockMovements.AsNoTracking();
        if (productId is int id)
            query = query.Where(m => m.ProductId == id);

        var movements = await query
            .OrderByDescending(m => m.CreatedAt)
            .ThenByDescending(m => m.Id)
            .Select(m => new
            {
                m.Id,
                m.ProductId,
                ProductName = m.Product.Name,
                m.Type,
                m.Quantity,
                m.Reason,
                m.Comment,
                m.PerformedBy,
                m.CreatedAt,
                m.ReversesMovementId
            })
            .ToListAsync(ct);

        // Which originals have already been reverted (so we don't offer a second reversal).
        var reverted = await _db.StockMovements.AsNoTracking()
            .Where(m => m.ReversesMovementId != null)
            .Select(m => m.ReversesMovementId!.Value)
            .ToListAsync(ct);
        var revertedSet = reverted.ToHashSet();

        return movements
            .Select(m =>
            {
                var isReversal = m.ReversesMovementId is not null;
                var isReversed = revertedSet.Contains(m.Id);
                return new StockMovementRow(
                    m.Id, m.ProductId, m.ProductName, TypeLabel(m.Type), m.Quantity,
                    ReasonLabel(m.Reason), m.Comment, m.PerformedBy, m.CreatedAt,
                    m.ReversesMovementId, isReversal, isReversed,
                    // Role is applied in the service; here only the data-level eligibility.
                    CanRevert: !isReversal && !isReversed);
            })
            .ToList();
    }

    public async Task<RevertOutcome> RevertMovementAsync(
        int movementId, string? performedBy, CancellationToken ct = default)
    {
        await using var tx = await _db.Database.BeginTransactionAsync(ct);
        try
        {
            var original = await _db.StockMovements.FirstOrDefaultAsync(m => m.Id == movementId, ct);
            if (original is null)
                return RevertOutcome.NotFound;

            if (original.Type is StockMovementType.ReversalReceipt or StockMovementType.ReversalWriteOff)
                return RevertOutcome.CannotRevertReversal;

            if (await _db.StockMovements.AnyAsync(m => m.ReversesMovementId == movementId, ct))
                return RevertOutcome.AlreadyReversed;

            var stock = await _db.StockItems.FirstOrDefaultAsync(s => s.ProductId == original.ProductId, ct)
                        ?? throw new InvalidOperationException($"Не найден остаток для товара #{original.ProductId}.");

            // The compensating entry carries the opposite sign of the original.
            var delta = -original.Quantity;

            // Undoing a receipt removes units — never below what's already reserved for orders.
            if (stock.Quantity + delta < stock.Reserved)
                return RevertOutcome.InsufficientStock;

            stock.Quantity += delta;

            var (reversalType, what) = original.Type == StockMovementType.Receipt
                ? (StockMovementType.ReversalReceipt, "приёмки")
                : (StockMovementType.ReversalWriteOff, "списания");

            _db.StockMovements.Add(new StockMovement
            {
                ProductId = original.ProductId,
                Type = reversalType,
                Quantity = delta,
                Reason = null,
                ReversesMovementId = original.Id,
                Comment = $"Откат {what} №{original.Id} от {original.CreatedAt:dd.MM.yyyy}",
                PerformedBy = performedBy
            });

            await _db.SaveChangesAsync(ct);
            await tx.CommitAsync(ct);
            return RevertOutcome.Success;
        }
        catch
        {
            await tx.RollbackAsync(ct);
            throw;
        }
    }

    private void AddMovement(
        int productId, StockMovementType type, int signedQuantity, WriteOffReason? reason, string? performedBy) =>
        _db.StockMovements.Add(new StockMovement
        {
            ProductId = productId,
            Type = type,
            Quantity = signedQuantity,
            Reason = reason,
            PerformedBy = performedBy
        });

    private static string TypeLabel(StockMovementType type) => type switch
    {
        StockMovementType.Receipt => "Приёмка",
        StockMovementType.WriteOff => "Списание",
        StockMovementType.ReversalReceipt => "Откат приёмки",
        StockMovementType.ReversalWriteOff => "Откат списания",
        _ => type.ToString()
    };

    private static string? ReasonLabel(WriteOffReason? reason) => reason switch
    {
        WriteOffReason.Defective => "Брак",
        WriteOffReason.Shortage => "Недостача",
        WriteOffReason.Damage => "Порча",
        WriteOffReason.Other => "Прочее",
        _ => null
    };
}