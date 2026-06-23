using System.Collections.ObjectModel;
using System.Globalization;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using StockFront.Contracts.Auth;
using StockFront.Contracts.Orders;
using StockFront.Contracts.Storefront;

namespace stockfront.ViewModels;

/// <summary>
/// The storefront screen: the catalog of orderable goods with category chips, a shopping cart overlay,
/// and checkout. Reads the catalog through <see cref="IStorefrontService"/> and places orders through
/// <see cref="IOrderService"/>; after a successful order it reloads so prices and availability stay in
/// step with the warehouse. Admins additionally get an inline card editor (name + price).
/// </summary>
public sealed partial class StorefrontViewModel : ObservableObject
{
    private const string FilterAll = "Все";

    private readonly IStorefrontService _storefront;
    private readonly IOrderService _orders;
    private readonly ICurrentUser _currentUser;

    // The full catalog; Products is the category-filtered view shown as cards.
    private IReadOnlyList<CatalogProductRow> _all = Array.Empty<CatalogProductRow>();

    public StorefrontViewModel(IStorefrontService storefront, IOrderService orders, ICurrentUser currentUser)
    {
        _storefront = storefront;
        _orders = orders;
        _currentUser = currentUser;
    }

    public ObservableCollection<CatalogProductRow> Products { get; } = new();
    public ObservableCollection<string> Filters { get; } = new();
    public ObservableCollection<CartLineViewModel> Cart { get; } = new();

    /// <summary>Admins may edit product cards in place; hidden for everyone else (see CLAUDE.md matrix).</summary>
    public bool CanEditCards => _currentUser.IsInRole(UserRole.Admin);

    [ObservableProperty] private bool _isLoading;
    [ObservableProperty] private string? _error;

    [ObservableProperty] private string _activeFilter = FilterAll;
    partial void OnActiveFilterChanged(string value) => ApplyFilter();

    [ObservableProperty] private int _cartCount;
    [ObservableProperty] private decimal _cartTotal;

    /// <summary>True while the cart has items — drives the checkout button's enabled state.</summary>
    public bool HasItems => Cart.Count > 0;

    [RelayCommand]
    public async Task LoadAsync()
    {
        IsLoading = true;
        Error = null;
        try
        {
            _all = await _storefront.GetCatalogAsync();
            RebuildFilters();
            ApplyFilter();
            ReconcileCart();
        }
        catch (Exception ex)
        {
            Error = $"Не удалось загрузить витрину: {ex.Message}";
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    private void SetFilter(string filter) => ActiveFilter = filter;

    private void RebuildFilters()
    {
        var categories = _all.Select(p => p.Category).Distinct().OrderBy(c => c);

        Filters.Clear();
        Filters.Add(FilterAll);
        foreach (var c in categories)
            Filters.Add(c);

        if (!Filters.Contains(ActiveFilter))
            ActiveFilter = FilterAll;
    }

    private void ApplyFilter()
    {
        IEnumerable<CatalogProductRow> rows = _all;
        if (ActiveFilter != FilterAll)
            rows = rows.Where(p => p.Category == ActiveFilter);

        Products.Clear();
        foreach (var row in rows)
            Products.Add(row);
    }

    // ── Cart ───────────────────────────────────────────────────────────────────────

    [RelayCommand]
    private void AddToCart(CatalogProductRow? product)
    {
        if (product is null || product.Availability == StorefrontAvailability.OutOfStock)
            return;

        var line = Cart.FirstOrDefault(l => l.ProductId == product.Id);
        if (line is null)
        {
            Cart.Add(new CartLineViewModel(product.Id, product.Name, product.Price, product.Available, 1));
        }
        else if (line.Quantity < line.Available)
        {
            line.Quantity++;
        }

        RecalcCart();
    }

    [RelayCommand]
    private void IncrementLine(CartLineViewModel? line)
    {
        if (line is not null && line.Quantity < line.Available)
        {
            line.Quantity++;
            RecalcCart();
        }
    }

    [RelayCommand]
    private void DecrementLine(CartLineViewModel? line)
    {
        if (line is null)
            return;

        if (line.Quantity > 1)
            line.Quantity--;
        else
            Cart.Remove(line);

        RecalcCart();
    }

    [RelayCommand]
    private void RemoveLine(CartLineViewModel? line)
    {
        if (line is not null)
        {
            Cart.Remove(line);
            RecalcCart();
        }
    }

    private void RecalcCart()
    {
        CartCount = Cart.Sum(l => l.Quantity);
        CartTotal = Cart.Sum(l => l.LineTotal);
        OnPropertyChanged(nameof(HasItems));
    }

    /// <summary>
    /// After a reload, drop cart lines whose product vanished and re-cap each remaining line to the
    /// current available stock, so the cart can never ask for more than the warehouse holds.
    /// </summary>
    private void ReconcileCart()
    {
        foreach (var line in Cart.ToList())
        {
            var current = _all.FirstOrDefault(p => p.Id == line.ProductId);
            if (current is null || current.Available <= 0)
            {
                Cart.Remove(line);
                continue;
            }

            line.Available = current.Available;
            line.UnitPrice = current.Price;
            if (line.Quantity > current.Available)
                line.Quantity = current.Available;
        }

        RecalcCart();
    }

    // ── Cart overlay + checkout ─────────────────────────────────────────────────────

    [ObservableProperty] private bool _isCartOpen;
    [ObservableProperty] private bool _checkoutBusy;
    [ObservableProperty] private string? _checkoutError;

    [ObservableProperty] private bool _isConfirmationOpen;
    [ObservableProperty] private string _confirmationText = "";

    [RelayCommand]
    private void OpenCart()
    {
        CheckoutError = null;
        IsCartOpen = true;
    }

    [RelayCommand]
    private void CloseCart() => IsCartOpen = false;

    [RelayCommand]
    private async Task CheckoutAsync()
    {
        CheckoutError = null;

        if (Cart.Count == 0)
        {
            CheckoutError = "Корзина пуста.";
            return;
        }

        CheckoutBusy = true;
        try
        {
            // ── Payment step (optional). The committed build has no gateway, so checkout places the
            //    order directly. To charge through the YooKassa sandbox, wire the drop-in gateway from
            //    /payments here (see /payments/README.md) before the call below. ──

            var customer = _currentUser.User?.DisplayName ?? "Гость";
            var items = Cart.Select(l => new CartItem(l.ProductId, l.Quantity)).ToList();

            var result = await _orders.PlaceOrderAsync(customer, items);
            if (!result.Succeeded)
            {
                CheckoutError = result.Error;
                return;
            }

            Cart.Clear();
            RecalcCart();
            IsCartOpen = false;

            ConfirmationText = $"Заказ №{result.OrderId} оформлен. Спасибо за покупку!";
            IsConfirmationOpen = true;

            // Stock changed — refresh the catalog (and re-cap any leftover cart lines).
            await LoadAsync();
        }
        finally
        {
            CheckoutBusy = false;
        }
    }

    [RelayCommand]
    private void CloseConfirmation() => IsConfirmationOpen = false;

    // ── Admin card editor ───────────────────────────────────────────────────────────

    [ObservableProperty] private bool _isEditOpen;
    [ObservableProperty] private CatalogProductRow? _editProduct;
    [ObservableProperty] private string _editName = "";
    [ObservableProperty] private string _editPrice = "";
    [ObservableProperty] private string? _editError;
    [ObservableProperty] private bool _editBusy;

    [RelayCommand]
    private void OpenEdit(CatalogProductRow? product)
    {
        if (product is null || !CanEditCards)
            return;

        EditProduct = product;
        EditName = product.Name;
        EditPrice = product.Price.ToString("0.##", CultureInfo.InvariantCulture);
        EditError = null;
        IsEditOpen = true;
    }

    [RelayCommand]
    private void CancelEdit() => IsEditOpen = false;

    [RelayCommand]
    private async Task SubmitEditAsync()
    {
        EditError = null;

        if (EditProduct is null)
            return;
        if (string.IsNullOrWhiteSpace(EditName))
        {
            EditError = "Укажите название товара.";
            return;
        }
        if (!TryParsePrice(EditPrice, out var price))
        {
            EditError = "Введите цену — число не меньше нуля.";
            return;
        }

        EditBusy = true;
        try
        {
            var result = await _storefront.UpdateProductCardAsync(EditProduct.Id, EditName, price);
            if (!result.Succeeded)
            {
                EditError = result.Error;
                return;
            }

            IsEditOpen = false;
            await LoadAsync();
        }
        finally
        {
            EditBusy = false;
        }
    }

    private static bool TryParsePrice(string text, out decimal price) =>
        decimal.TryParse(text?.Trim().Replace(',', '.'), NumberStyles.Number, CultureInfo.InvariantCulture, out price)
        && price >= 0;
}

/// <summary>One line of the shopping cart: a product, its unit price and the chosen quantity (capped at
/// the available stock). <see cref="LineTotal"/> tracks <see cref="Quantity"/> for the cart display.</summary>
public sealed partial class CartLineViewModel : ObservableObject
{
    public CartLineViewModel(int productId, string name, decimal unitPrice, int available, int quantity)
    {
        ProductId = productId;
        Name = name;
        _unitPrice = unitPrice;
        _available = available;
        _quantity = quantity;
    }

    public int ProductId { get; }
    public string Name { get; }

    [ObservableProperty] private decimal _unitPrice;
    [ObservableProperty] private int _available;
    [ObservableProperty] private int _quantity;

    public decimal LineTotal => UnitPrice * Quantity;

    partial void OnQuantityChanged(int value) => OnPropertyChanged(nameof(LineTotal));
    partial void OnUnitPriceChanged(decimal value) => OnPropertyChanged(nameof(LineTotal));
}