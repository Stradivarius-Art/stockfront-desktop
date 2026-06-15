using StockFront.Contracts.Auth;
using StockFront.Contracts.Dashboard;
using StockFront.Contracts.Persistence;
using StockFront.Contracts.Warehouse;

namespace StockFront.Warehouse.Services;

/// <summary>
/// Warehouse Accounting business logic. Validates receipts and write-offs, classifies stock into the
/// availability traffic light, and stamps each movement with the operator who performed it. All data
/// access goes through <see cref="IWarehouseRepository"/> — this class never touches the database or
/// EF directly, so it can be unit-tested with a mocked repository.
/// </summary>
public sealed class WarehouseService : IWarehouseService
{
    /// <summary>At or below this many available units a product counts as "low" (мало).</summary>
    private const int LowStockThreshold = 15;

    private readonly IWarehouseRepository _repo;
    private readonly ICurrentUser _currentUser;

    public WarehouseService(IWarehouseRepository repo, ICurrentUser currentUser)
    {
        _repo = repo;
        _currentUser = currentUser;
    }

    public async Task<IReadOnlyList<WarehouseProductRow>> GetProductsAsync(CancellationToken ct = default)
    {
        var rows = await _repo.GetProductsAsync(ct);
        return rows
            .Select(r =>
            {
                var available = r.Quantity - r.Reserved;
                return new WarehouseProductRow(
                    r.Id, r.Sku, r.Name, r.Category, r.Quantity, r.Reserved, available, r.Price,
                    Classify(available));
            })
            .ToList();
    }

    public Task<IReadOnlyList<CategoryOption>> GetCategoriesAsync(CancellationToken ct = default) =>
        _repo.GetCategoriesAsync(ct);

    public async Task<WarehouseResult> ReceiveAsync(int productId, int quantity, CancellationToken ct = default)
    {
        if (quantity <= 0)
            return WarehouseResult.Fail("Количество должно быть больше нуля.");

        await _repo.ReceiveAsync(productId, quantity, PerformedBy, ct);
        return WarehouseResult.Ok();
    }

    public async Task<WarehouseResult> ReceiveNewProductAsync(
        string sku, string name, int categoryId, decimal price, int quantity, CancellationToken ct = default)
    {
        sku = sku?.Trim() ?? "";
        name = name?.Trim() ?? "";

        if (sku.Length == 0)
            return WarehouseResult.Fail("Укажите артикул.");
        if (name.Length == 0)
            return WarehouseResult.Fail("Укажите название товара.");
        if (categoryId <= 0)
            return WarehouseResult.Fail("Выберите категорию.");
        if (price < 0)
            return WarehouseResult.Fail("Цена не может быть отрицательной.");
        if (quantity <= 0)
            return WarehouseResult.Fail("Количество должно быть больше нуля.");

        if (await _repo.SkuExistsAsync(sku, ct))
            return WarehouseResult.Fail($"Товар с артикулом «{sku}» уже существует.");

        await _repo.CreateProductWithStockAsync(sku, name, categoryId, price, quantity, PerformedBy, ct);
        return WarehouseResult.Ok();
    }

    public async Task<WarehouseResult> WriteOffAsync(
        int productId, int quantity, WriteOffReason reason, CancellationToken ct = default)
    {
        if (quantity <= 0)
            return WarehouseResult.Fail("Количество должно быть больше нуля.");

        var ok = await _repo.WriteOffAsync(productId, quantity, reason, PerformedBy, ct);
        return ok
            ? WarehouseResult.Ok()
            : WarehouseResult.Fail("Недостаточно товара на складе для списания.");
    }

    private string? PerformedBy => _currentUser.User?.DisplayName;

    private static StockStatus Classify(int available) =>
        available <= 0 ? StockStatus.OutOfStock
        : available <= LowStockThreshold ? StockStatus.Low
        : StockStatus.InStock;
}