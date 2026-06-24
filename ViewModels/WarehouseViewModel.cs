using System.Collections.ObjectModel;
using System.Globalization;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using StockFront.Contracts.Auth;
using StockFront.Contracts.Dashboard;
using StockFront.Contracts.Warehouse;

namespace stockfront.ViewModels;

/// <summary>
/// The warehouse screen: the stock table with category/status chips and search, plus two inline
/// overlay forms for receiving goods and writing them off. Reads and changes stock through
/// <see cref="IWarehouseService"/>; after a successful change it reloads so the table and badges
/// stay in step with the database.
/// </summary>
public sealed partial class WarehouseViewModel : ObservableObject
{
    // Built-in status chips that always precede the per-category ones. One per actionable bucket of
    // the Days-of-Supply traffic light.
    private const string FilterAll = "Все";
    private const string FilterCritical = "Критично";
    private const string FilterLow = "Мало";
    private const string FilterOut = "Нет";
    private const string FilterOverstock = "Избыток";
    private const string FilterIlliquid = "Неликвид";

    private readonly IWarehouseService _warehouse;
    private readonly ICurrentUser _currentUser;

    // The full, unfiltered list; Products is the filtered view shown in the table.
    private IReadOnlyList<WarehouseProductRow> _all = Array.Empty<WarehouseProductRow>();

    public WarehouseViewModel(IWarehouseService warehouse, ICurrentUser currentUser)
    {
        _warehouse = warehouse;
        _currentUser = currentUser;
    }

    /// <summary>Editing a product's nomenclature (name/price/category) is admin-only (see CLAUDE.md matrix).</summary>
    public bool CanEditProducts => _currentUser.IsInRole(UserRole.Admin);

    public ObservableCollection<WarehouseProductRow> Products { get; } = new();

    /// <summary>Source for the "existing product" pickers in both overlay forms.</summary>
    public ObservableCollection<WarehouseProductRow> AllProducts { get; } = new();

    /// <summary>Status options for the "Статус" dropdown (the fixed Days-of-Supply buckets).</summary>
    public ObservableCollection<string> Filters { get; } = new();

    /// <summary>Category names for the rounded filter dropdown ("Все" + one per category in the data).</summary>
    public ObservableCollection<string> CategoryFilters { get; } = new();

    public ObservableCollection<CategoryOption> Categories { get; } = new();

    /// <summary>Write-off reasons paired with their Russian labels for the dropdown.</summary>
    public IReadOnlyList<ReasonOption> WriteOffReasons { get; } = new[]
    {
        new ReasonOption(WriteOffReason.Defective, "Брак"),
        new ReasonOption(WriteOffReason.Shortage, "Недостача"),
        new ReasonOption(WriteOffReason.Damage, "Порча"),
        new ReasonOption(WriteOffReason.Other, "Прочее"),
    };

    [ObservableProperty] private bool _isLoading;
    [ObservableProperty] private string? _error;

    [ObservableProperty] private string _activeFilter = FilterAll;
    [ObservableProperty] private string _searchText = "";

    partial void OnActiveFilterChanged(string value)
    {
        ApplyFilter();
        // Status and category share one filter, so when one dropdown changes the other reads back "Все".
        OnPropertyChanged(nameof(SelectedCategoryFilter));
        OnPropertyChanged(nameof(SelectedStatusFilter));
    }

    partial void OnSearchTextChanged(string value) => ApplyFilter();

    /// <summary>
    /// Two-way proxy over <see cref="ActiveFilter"/> for the category dropdown: reads back as "Все"
    /// whenever a status (not a category) is the active filter, so status and category stay mutually
    /// exclusive on one shared filter.
    /// </summary>
    public string SelectedCategoryFilter
    {
        get => CategoryFilters.Contains(ActiveFilter) ? ActiveFilter : FilterAll;
        set => ActiveFilter = value;
    }

    /// <summary>
    /// Two-way proxy over <see cref="ActiveFilter"/> for the status dropdown — the counterpart of
    /// <see cref="SelectedCategoryFilter"/>; reads back "Все" whenever a category is active.
    /// </summary>
    public string SelectedStatusFilter
    {
        get => Filters.Contains(ActiveFilter) ? ActiveFilter : FilterAll;
        set => ActiveFilter = value;
    }

    [RelayCommand]
    public async Task LoadAsync()
    {
        IsLoading = true;
        Error = null;
        try
        {
            _all = await _warehouse.GetProductsAsync();

            AllProducts.Clear();
            foreach (var row in _all)
                AllProducts.Add(row);

            RebuildFilters();
            ApplyFilter();

            Categories.Clear();
            foreach (var c in await _warehouse.GetCategoriesAsync())
                Categories.Add(c);
        }
        catch (Exception ex)
        {
            Error = $"Не удалось загрузить склад: {ex.Message}";
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    private void SetFilter(string filter) => ActiveFilter = filter;

    // Status buckets and categories each get their own dropdown so the filter row never overflows.
    private void RebuildFilters()
    {
        var categories = _all.Select(p => p.Category).Distinct().OrderBy(c => c);

        Filters.Clear();
        Filters.Add(FilterAll);
        Filters.Add(FilterCritical);
        Filters.Add(FilterLow);
        Filters.Add(FilterOut);
        Filters.Add(FilterOverstock);
        Filters.Add(FilterIlliquid);

        CategoryFilters.Clear();
        CategoryFilters.Add(FilterAll);
        foreach (var c in categories)
            CategoryFilters.Add(c);

        if (!Filters.Contains(ActiveFilter) && !CategoryFilters.Contains(ActiveFilter))
            ActiveFilter = FilterAll;

        OnPropertyChanged(nameof(SelectedCategoryFilter));
        OnPropertyChanged(nameof(SelectedStatusFilter));
    }

    private void ApplyFilter()
    {
        IEnumerable<WarehouseProductRow> rows = _all;

        rows = ActiveFilter switch
        {
            FilterAll => rows,
            FilterCritical => rows.Where(p => p.Status == StockStatus.Critical),
            FilterLow => rows.Where(p => p.Status == StockStatus.Low),
            FilterOut => rows.Where(p => p.Status == StockStatus.OutOfStock),
            FilterOverstock => rows.Where(p => p.Status == StockStatus.Overstock),
            FilterIlliquid => rows.Where(p => p.Status == StockStatus.Illiquid),
            _ => rows.Where(p => p.Category == ActiveFilter),
        };

        var query = SearchText?.Trim();
        if (!string.IsNullOrEmpty(query))
            rows = rows.Where(p =>
                p.Name.Contains(query, StringComparison.OrdinalIgnoreCase) ||
                p.Sku.Contains(query, StringComparison.OrdinalIgnoreCase));

        Products.Clear();
        foreach (var row in rows)
            Products.Add(row);
    }

    // ── Receiving (приёмка) ────────────────────────────────────────────────────────

    [ObservableProperty] private bool _isReceiveOpen;
    [ObservableProperty] private bool _receiveIsNewProduct;
    [ObservableProperty] private WarehouseProductRow? _receiveSelectedProduct;
    [ObservableProperty] private string _receiveQuantity = "";
    [ObservableProperty] private string _receiveNewSku = "";
    [ObservableProperty] private string _receiveNewName = "";
    [ObservableProperty] private CategoryOption? _receiveSelectedCategory;
    [ObservableProperty] private string _receiveNewPrice = "";
    [ObservableProperty] private string? _receiveError;
    [ObservableProperty] private bool _receiveBusy;

    [RelayCommand]
    private void OpenReceive()
    {
        ReceiveIsNewProduct = false;
        ReceiveSelectedProduct = null;
        ReceiveQuantity = "";
        ReceiveNewSku = "";
        ReceiveNewName = "";
        ReceiveSelectedCategory = null;
        ReceiveNewPrice = "";
        ReceiveError = null;
        IsReceiveOpen = true;
    }

    [RelayCommand]
    private void CancelReceive() => IsReceiveOpen = false;

    [RelayCommand]
    private async Task SubmitReceiveAsync()
    {
        ReceiveError = null;

        if (!TryParseQuantity(ReceiveQuantity, out var quantity))
        {
            ReceiveError = "Введите количество — целое число больше нуля.";
            return;
        }

        ReceiveBusy = true;
        try
        {
            WarehouseResult result;
            if (ReceiveIsNewProduct)
            {
                if (ReceiveSelectedCategory is null)
                {
                    ReceiveError = "Выберите категорию.";
                    return;
                }
                if (!TryParsePrice(ReceiveNewPrice, out var price))
                {
                    ReceiveError = "Введите цену — число не меньше нуля.";
                    return;
                }

                result = await _warehouse.ReceiveNewProductAsync(
                    ReceiveNewSku, ReceiveNewName, ReceiveSelectedCategory.Id, price, quantity);
            }
            else
            {
                if (ReceiveSelectedProduct is null)
                {
                    ReceiveError = "Выберите товар.";
                    return;
                }

                result = await _warehouse.ReceiveAsync(ReceiveSelectedProduct.Id, quantity);
            }

            if (!result.Succeeded)
            {
                ReceiveError = result.Error;
                return;
            }

            IsReceiveOpen = false;
            await LoadAsync();
        }
        finally
        {
            ReceiveBusy = false;
        }
    }

    // ── Writing off (списание) ─────────────────────────────────────────────────────

    [ObservableProperty] private bool _isWriteOffOpen;
    [ObservableProperty] private WarehouseProductRow? _writeOffSelectedProduct;
    [ObservableProperty] private string _writeOffQuantity = "";
    [ObservableProperty] private ReasonOption? _writeOffSelectedReason;
    [ObservableProperty] private string? _writeOffError;
    [ObservableProperty] private bool _writeOffBusy;

    [RelayCommand]
    private void OpenWriteOff()
    {
        WriteOffSelectedProduct = null;
        WriteOffQuantity = "";
        WriteOffSelectedReason = WriteOffReasons[0];
        WriteOffError = null;
        IsWriteOffOpen = true;
    }

    [RelayCommand]
    private void CancelWriteOff() => IsWriteOffOpen = false;

    [RelayCommand]
    private async Task SubmitWriteOffAsync()
    {
        WriteOffError = null;

        if (WriteOffSelectedProduct is null)
        {
            WriteOffError = "Выберите товар.";
            return;
        }
        if (!TryParseQuantity(WriteOffQuantity, out var quantity))
        {
            WriteOffError = "Введите количество — целое число больше нуля.";
            return;
        }
        if (WriteOffSelectedReason is null)
        {
            WriteOffError = "Выберите причину.";
            return;
        }

        WriteOffBusy = true;
        try
        {
            var result = await _warehouse.WriteOffAsync(
                WriteOffSelectedProduct.Id, quantity, WriteOffSelectedReason.Value);

            if (!result.Succeeded)
            {
                WriteOffError = result.Error;
                return;
            }

            IsWriteOffOpen = false;
            await LoadAsync();
        }
        finally
        {
            WriteOffBusy = false;
        }
    }

    // ── Editing a product (nomenclature: name, price, category) ─────────────────────

    [ObservableProperty] private bool _isEditProductOpen;
    [ObservableProperty] private WarehouseProductRow? _editProductTarget;
    [ObservableProperty] private string _editProductName = "";
    [ObservableProperty] private string _editProductPrice = "";
    [ObservableProperty] private CategoryOption? _editProductCategory;
    [ObservableProperty] private string? _editProductError;
    [ObservableProperty] private bool _editProductBusy;

    [RelayCommand]
    private void OpenEditProduct(WarehouseProductRow? product)
    {
        if (product is null || !CanEditProducts)
            return;

        EditProductTarget = product;
        EditProductName = product.Name;
        EditProductPrice = product.Price.ToString("0.##", CultureInfo.InvariantCulture);
        // Preselect the product's current category by name (the row carries the name, not the id).
        EditProductCategory = Categories.FirstOrDefault(c => c.Name == product.Category);
        EditProductError = null;
        IsEditProductOpen = true;
    }

    [RelayCommand]
    private void CancelEditProduct() => IsEditProductOpen = false;

    [RelayCommand]
    private async Task SubmitEditProductAsync()
    {
        EditProductError = null;

        if (EditProductTarget is null)
            return;
        if (string.IsNullOrWhiteSpace(EditProductName))
        {
            EditProductError = "Укажите название товара.";
            return;
        }
        if (!TryParsePrice(EditProductPrice, out var price))
        {
            EditProductError = "Введите цену — число не меньше нуля.";
            return;
        }
        if (EditProductCategory is null)
        {
            EditProductError = "Выберите категорию.";
            return;
        }

        EditProductBusy = true;
        try
        {
            var result = await _warehouse.UpdateProductDetailsAsync(
                EditProductTarget.Id, EditProductName, price, EditProductCategory.Id);
            if (!result.Succeeded)
            {
                EditProductError = result.Error;
                return;
            }

            IsEditProductOpen = false;
            await LoadAsync();
        }
        finally
        {
            EditProductBusy = false;
        }
    }

    // ── Deleting a product (admin only; offered only when stock is 0) ───────────────

    [ObservableProperty] private bool _isDeleteConfirmOpen;
    [ObservableProperty] private WarehouseProductRow? _deleteTarget;
    [ObservableProperty] private string? _deleteError;
    [ObservableProperty] private bool _deleteBusy;

    [RelayCommand]
    private void RequestDeleteProduct(WarehouseProductRow? product)
    {
        if (product is null || !CanEditProducts)
            return;

        DeleteTarget = product;
        DeleteError = null;
        IsDeleteConfirmOpen = true;
    }

    [RelayCommand]
    private void CancelDeleteProduct() => IsDeleteConfirmOpen = false;

    [RelayCommand]
    private async Task ConfirmDeleteProductAsync()
    {
        if (DeleteTarget is null)
            return;

        DeleteBusy = true;
        try
        {
            var result = await _warehouse.DeleteProductAsync(DeleteTarget.Id);
            if (!result.Succeeded)
            {
                DeleteError = result.Error;
                return;
            }

            IsDeleteConfirmOpen = false;
            await LoadAsync();
        }
        finally
        {
            DeleteBusy = false;
        }
    }

    // ── Operations journal (история операций + откат) ──────────────────────────────

    /// <summary>The stock-movement history shown in the journal overlay (newest first).</summary>
    public ObservableCollection<StockMovementRow> Movements { get; } = new();

    [ObservableProperty] private bool _isJournalOpen;
    [ObservableProperty] private string? _journalError;

    [RelayCommand]
    private async Task OpenJournalAsync()
    {
        JournalError = null;
        IsJournalOpen = true;
        await LoadMovementsAsync();
    }

    [RelayCommand]
    private void CloseJournal() => IsJournalOpen = false;

    private async Task LoadMovementsAsync()
    {
        Movements.Clear();
        foreach (var m in await _warehouse.GetMovementsAsync())
            Movements.Add(m);
    }

    [RelayCommand]
    private async Task RevertMovementAsync(StockMovementRow? movement)
    {
        if (movement is null)
            return;

        JournalError = null;
        var result = await _warehouse.RevertMovementAsync(movement.Id);
        if (!result.Succeeded)
        {
            JournalError = result.Error;
            return;
        }

        // Stock changed: refresh both the journal and the table behind it.
        await LoadMovementsAsync();
        await LoadAsync();
    }

    private static bool TryParseQuantity(string text, out int quantity) =>
        int.TryParse(text?.Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out quantity)
        && quantity > 0;

    private static bool TryParsePrice(string text, out decimal price) =>
        decimal.TryParse(text?.Trim().Replace(',', '.'), NumberStyles.Number, CultureInfo.InvariantCulture, out price)
        && price >= 0;
}

/// <summary>A write-off reason with its display label, for the reason dropdown.</summary>
public sealed record ReasonOption(WriteOffReason Value, string Label);