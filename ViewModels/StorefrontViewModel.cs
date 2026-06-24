using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using StockFront.Contracts.Auth;
using StockFront.Contracts.Orders;
using StockFront.Contracts.Storefront;
using stockfront.Infrastructure;

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

    // At or below this many cards the centred grid looks lost, so the catalog switches to a list.
    private const int ListThreshold = 4;

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

    /// <summary>Customers don't see out-of-stock goods at all; staff/admin still see the full catalog.</summary>
    private bool HideOutOfStock => _currentUser.IsInRole(UserRole.Customer);

    /// <summary>With only a few products a list reads better than a sparse centred grid of cards.</summary>
    public bool IsListView => Products.Count is > 0 and <= ListThreshold;
    public bool IsGridView => !IsListView;

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
        if (HideOutOfStock)
            rows = rows.Where(p => p.Availability != StorefrontAvailability.OutOfStock);
        if (ActiveFilter != FilterAll)
            rows = rows.Where(p => p.Category == ActiveFilter);

        Products.Clear();
        foreach (var row in rows)
            Products.Add(row);

        OnPropertyChanged(nameof(IsListView));
        OnPropertyChanged(nameof(IsGridView));
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
        else
        {
            // Already at the stock cap — tell the user instead of silently doing nothing.
            _ = ShowToastAsync($"«{product.Name}»: больше нет в наличии");
            return;
        }

        RecalcCart();
        _ = ShowToastAsync($"«{product.Name}» добавлен в корзину");
    }

    // ── "Added to cart" toast ───────────────────────────────────────────────────────

    [ObservableProperty] private bool _isToastVisible;
    [ObservableProperty] private string _toastText = "";
    private CancellationTokenSource? _toastCts;

    /// <summary>Show a brief auto-dismissing toast; a new message resets the timer.</summary>
    private async Task ShowToastAsync(string text)
    {
        ToastText = text;
        IsToastVisible = true;

        _toastCts?.Cancel();
        var cts = _toastCts = new CancellationTokenSource();
        try
        {
            await Task.Delay(2200, cts.Token);
            IsToastVisible = false;
        }
        catch (OperationCanceledException)
        {
            // Superseded by a newer toast; the later call owns the visibility.
        }
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

    // ── Admin card editor (storefront presentation: image + description) ──────────────

    [ObservableProperty] private bool _isEditOpen;
    [ObservableProperty] private CatalogProductRow? _editProduct;
    [ObservableProperty] private string _editProductName = "";
    [ObservableProperty] private string? _editImagePath;
    [ObservableProperty] private string _editDescription = "";
    [ObservableProperty] private string? _editError;
    [ObservableProperty] private bool _editBusy;

    /// <summary>True once an image has been chosen — drives the preview vs. placeholder in the editor.</summary>
    public bool HasEditImage => !string.IsNullOrWhiteSpace(EditImagePath);
    partial void OnEditImagePathChanged(string? value) => OnPropertyChanged(nameof(HasEditImage));

    [RelayCommand]
    private void OpenEdit(CatalogProductRow? product)
    {
        if (product is null || !CanEditCards)
            return;

        EditProduct = product;
        EditProductName = product.Name;
        EditImagePath = product.ImagePath;
        EditDescription = product.Description ?? "";
        EditError = null;
        IsEditOpen = true;
    }

    /// <summary>Pick an image file and copy it into the local image store; keep only its stored name.</summary>
    [RelayCommand]
    private void PickImage()
    {
        var dialog = new Microsoft.Win32.OpenFileDialog
        {
            Title = "Выберите изображение товара",
            Filter = "Изображения|*.png;*.jpg;*.jpeg;*.bmp;*.gif;*.webp|Все файлы|*.*"
        };
        if (dialog.ShowDialog() != true)
            return;

        try
        {
            EditImagePath = ImageStore.Save(dialog.FileName);
            EditError = null;
        }
        catch (Exception ex)
        {
            EditError = $"Не удалось загрузить изображение: {ex.Message}";
        }
    }

    [RelayCommand]
    private void ClearImage() => EditImagePath = null;

    [RelayCommand]
    private void CancelEdit() => IsEditOpen = false;

    [RelayCommand]
    private async Task SubmitEditAsync()
    {
        EditError = null;

        if (EditProduct is null)
            return;

        EditBusy = true;
        try
        {
            var result = await _storefront.UpdateProductPresentationAsync(
                EditProduct.Id, EditImagePath, EditDescription);
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