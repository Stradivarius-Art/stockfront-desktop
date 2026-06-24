using StockFront.Contracts.Auth;
using StockFront.Contracts.Persistence;
using StockFront.Contracts.Warehouse;

namespace StockFront.Warehouse.Services;

/// <summary>
/// Warehouse Accounting business logic. Validates receipts and write-offs, derives the availability
/// traffic light from Days of Supply (via <see cref="StockStatusCalculator"/>), and stamps each
/// movement with the operator who performed it. Reverting a movement is restricted to administrators.
/// All data access goes through <see cref="IWarehouseRepository"/> — this class never touches the
/// database or EF directly, so it can be unit-tested with a mocked repository.
/// </summary>
public sealed class WarehouseService : IWarehouseService
{
    private readonly IWarehouseRepository _repo;
    private readonly ICurrentUser _currentUser;

    public WarehouseService(IWarehouseRepository repo, ICurrentUser currentUser)
    {
        _repo = repo;
        _currentUser = currentUser;
    }

    public async Task<IReadOnlyList<WarehouseProductRow>> GetProductsAsync(CancellationToken ct = default)
    {
        var now = DateTime.UtcNow;
        var rows = await _repo.GetProductsAsync(ct);
        return rows
            .Select(r =>
            {
                var available = r.Quantity - r.Reserved;
                var status = StockStatusCalculator.Classify(new StockStatusInputs(
                    r.Quantity, r.AvgDailySales, r.LastSale, r.CreatedAt,
                    r.LeadTimeDays, r.SafetyBufferDays, r.IsSeasonal, now));

                return new WarehouseProductRow(
                    r.Id, r.Sku, r.Name, r.Category, r.Quantity, r.Reserved, available, r.Price,
                    status, r.AvgDailySales,
                    StockStatusCalculator.DaysOfSupply(r.Quantity, r.AvgDailySales),
                    r.Description, r.ImagePath);
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

    public async Task<WarehouseResult> UpdateProductDetailsAsync(
        int productId, string name, decimal price, int categoryId, CancellationToken ct = default)
    {
        name = name?.Trim() ?? "";

        if (name.Length == 0)
            return WarehouseResult.Fail("Укажите название товара.");
        if (price < 0)
            return WarehouseResult.Fail("Цена не может быть отрицательной.");
        if (categoryId <= 0)
            return WarehouseResult.Fail("Выберите категорию.");

        var ok = await _repo.UpdateProductDetailsAsync(productId, name, price, categoryId, ct);
        return ok
            ? WarehouseResult.Ok()
            : WarehouseResult.Fail("Товар не найден.");
    }

    public async Task<WarehouseResult> UpdateProductPresentationAsync(
        int productId, string? imagePath, string? description, CancellationToken ct = default)
    {
        // Presentation text/image are optional; normalise empty input to null so the column stays clean.
        imagePath = string.IsNullOrWhiteSpace(imagePath) ? null : imagePath.Trim();
        description = string.IsNullOrWhiteSpace(description) ? null : description.Trim();

        var ok = await _repo.UpdateProductPresentationAsync(productId, imagePath, description, ct);
        return ok
            ? WarehouseResult.Ok()
            : WarehouseResult.Fail("Товар не найден.");
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

    public async Task<WarehouseResult> DeleteProductAsync(int productId, CancellationToken ct = default)
    {
        if (!_currentUser.IsInRole(UserRole.Admin))
            return WarehouseResult.Fail("Удаление товаров доступно только администратору.");

        var outcome = await _repo.DeleteProductAsync(productId, ct);
        return outcome switch
        {
            DeleteProductOutcome.Success => WarehouseResult.Ok(),
            DeleteProductOutcome.NotFound => WarehouseResult.Fail("Товар не найден."),
            DeleteProductOutcome.HasStock =>
                WarehouseResult.Fail("Удалить можно только товар с нулевым остатком."),
            DeleteProductOutcome.InUse =>
                WarehouseResult.Fail("Товар присутствует в заказах — удалить нельзя."),
            _ => WarehouseResult.Fail("Не удалось удалить товар.")
        };
    }

    public async Task<IReadOnlyList<StockMovementRow>> GetMovementsAsync(
        int? productId = null, CancellationToken ct = default)
    {
        var rows = await _repo.GetMovementsAsync(productId, ct);

        // Reverting is an administrator-only action; fold the role into each row's CanRevert flag so
        // the UI can bind it directly without knowing the role.
        var isAdmin = _currentUser.IsInRole(UserRole.Admin);
        return rows
            .Select(r => r with { CanRevert = r.CanRevert && isAdmin })
            .ToList();
    }

    public async Task<WarehouseResult> RevertMovementAsync(int movementId, CancellationToken ct = default)
    {
        if (!_currentUser.IsInRole(UserRole.Admin))
            return WarehouseResult.Fail("Откат операций доступен только администратору.");

        var outcome = await _repo.RevertMovementAsync(movementId, PerformedBy, ct);
        return outcome switch
        {
            RevertOutcome.Success => WarehouseResult.Ok(),
            RevertOutcome.NotFound => WarehouseResult.Fail("Операция не найдена."),
            RevertOutcome.AlreadyReversed => WarehouseResult.Fail("Эта операция уже была отменена."),
            RevertOutcome.CannotRevertReversal => WarehouseResult.Fail("Нельзя откатить запись отката."),
            RevertOutcome.InsufficientStock =>
                WarehouseResult.Fail("Откат невозможен: на складе недостаточно товара (часть зарезервирована)."),
            _ => WarehouseResult.Fail("Не удалось выполнить откат.")
        };
    }

    private string? PerformedBy => _currentUser.User?.DisplayName;
}