using System.Collections.ObjectModel;
using System.Globalization;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
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
    // Built-in chips that always precede the per-category ones.
    private const string FilterAll = "Все";
    private const string FilterLow = "Низкий остаток";
    private const string FilterOut = "Нет в наличии";

    private readonly IWarehouseService _warehouse;

    // The full, unfiltered list; Products is the filtered view shown in the table.
    private IReadOnlyList<WarehouseProductRow> _all = Array.Empty<WarehouseProductRow>();

    public WarehouseViewModel(IWarehouseService warehouse) => _warehouse = warehouse;

    public ObservableCollection<WarehouseProductRow> Products { get; } = new();

    /// <summary>Source for the "existing product" pickers in both overlay forms.</summary>
    public ObservableCollection<WarehouseProductRow> AllProducts { get; } = new();

    public ObservableCollection<string> Filters { get; } = new();
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

    partial void OnActiveFilterChanged(string value) => ApplyFilter();
    partial void OnSearchTextChanged(string value) => ApplyFilter();

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

    // Chips: the three built-ins, then one per category present in the data.
    private void RebuildFilters()
    {
        var categories = _all.Select(p => p.Category).Distinct().OrderBy(c => c);

        Filters.Clear();
        Filters.Add(FilterAll);
        Filters.Add(FilterLow);
        Filters.Add(FilterOut);
        foreach (var c in categories)
            Filters.Add(c);

        if (!Filters.Contains(ActiveFilter))
            ActiveFilter = FilterAll;
    }

    private void ApplyFilter()
    {
        IEnumerable<WarehouseProductRow> rows = _all;

        rows = ActiveFilter switch
        {
            FilterAll => rows,
            FilterLow => rows.Where(p => p.Status == StockStatus.Low),
            FilterOut => rows.Where(p => p.Status == StockStatus.OutOfStock),
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

    private static bool TryParseQuantity(string text, out int quantity) =>
        int.TryParse(text?.Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out quantity)
        && quantity > 0;

    private static bool TryParsePrice(string text, out decimal price) =>
        decimal.TryParse(text?.Trim().Replace(',', '.'), NumberStyles.Number, CultureInfo.InvariantCulture, out price)
        && price >= 0;
}

/// <summary>A write-off reason with its display label, for the reason dropdown.</summary>
public sealed record ReasonOption(WriteOffReason Value, string Label);