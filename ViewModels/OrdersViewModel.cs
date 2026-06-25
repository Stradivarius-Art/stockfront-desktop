using System.Collections.ObjectModel;
using System.Globalization;
using System.IO;
using System.Text;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using StockFront.Contracts.Auth;
using StockFront.Contracts.Orders;

namespace stockfront.ViewModels;

/// <summary>
/// The orders screen: the list of orders with a status filter dropdown and CSV export. What the screen
/// can do depends on the signed-in role (see the matrix in CLAUDE.md): an admin manages every order
/// through its whole lifecycle, a warehouse keeper only ships paid orders, and a customer sees just
/// their own orders read-only. Reads and changes orders through <see cref="IOrderService"/>.
/// </summary>
public sealed partial class OrdersViewModel : ObservableObject
{
    private const string FilterAll = "Все";

    private readonly IOrderService _orders;
    private readonly ICurrentUser _currentUser;

    // The full, role-scoped list; Orders is the status-filtered view shown in the table.
    private IReadOnlyList<OrderRowViewModel> _all = Array.Empty<OrderRowViewModel>();

    public OrdersViewModel(IOrderService orders, ICurrentUser currentUser)
    {
        _orders = orders;
        _currentUser = currentUser;
    }

    public ObservableCollection<OrderRowViewModel> Orders { get; } = new();

    /// <summary>The status filter options, paired with the status they select (null = "Все").</summary>
    public IReadOnlyList<StatusFilterOption> Filters { get; } = new[]
    {
        new StatusFilterOption(FilterAll, null),
        new StatusFilterOption("Новые", OrderStatus.New),
        new StatusFilterOption("Резерв", OrderStatus.Reserved),
        new StatusFilterOption("Оплачены", OrderStatus.Paid),
        new StatusFilterOption("Отгружены", OrderStatus.Shipped),
        new StatusFilterOption("Завершены", OrderStatus.Completed),
        new StatusFilterOption("Отменены", OrderStatus.Cancelled),
    };

    /// <summary>Admin manages the whole lifecycle of any order (advance + cancel).</summary>
    public bool CanManageOrders => _currentUser.IsInRole(UserRole.Admin);

    /// <summary>A warehouse keeper may only move a paid order to «Отгружена» (see CLAUDE.md matrix).</summary>
    public bool CanShipOrders => _currentUser.IsInRole(UserRole.WarehouseKeeper);

    /// <summary>Whether any role can act on rows — drives the (otherwise collapsed) actions column.</summary>
    public bool ShowActions => CanManageOrders || CanShipOrders;

    [ObservableProperty] private bool _isLoading;
    [ObservableProperty] private string? _error;

    /// <summary>True when the table has no rows to show — drives the empty-state message.</summary>
    [ObservableProperty] private bool _isEmpty;

    /// <summary>The empty-state caption: distinguishes "no orders at all" from "none match the filter".</summary>
    [ObservableProperty] private string _emptyText = "Заказов пока нет";

    [ObservableProperty] private StatusFilterOption? _selectedFilter;
    partial void OnSelectedFilterChanged(StatusFilterOption? value) => ApplyFilter();

    [RelayCommand]
    public async Task LoadAsync()
    {
        IsLoading = true;
        Error = null;
        try
        {
            // A customer sees only their own orders, keyed by the display name their orders are stamped
            // with at checkout; staff and admin see everything.
            var scope = _currentUser.IsInRole(UserRole.Customer)
                ? _currentUser.User?.DisplayName
                : null;

            var rows = await _orders.GetOrdersAsync(scope);
            _all = rows.Select(r => new OrderRowViewModel(r, CanManageOrders, CanShipOrders)).ToList();

            SelectedFilter ??= Filters[0];
            ApplyFilter();
        }
        catch (Exception ex)
        {
            Error = $"Не удалось загрузить заказы: {ex.Message}";
        }
        finally
        {
            IsLoading = false;
        }
    }

    private void ApplyFilter()
    {
        var status = SelectedFilter?.Status;
        var rows = status is null ? _all : _all.Where(o => o.Status == status);

        Orders.Clear();
        foreach (var row in rows)
            Orders.Add(row);

        IsEmpty = Orders.Count == 0;
        // "Nothing at all" vs "nothing for this filter" — so the message matches what the user did.
        EmptyText = _all.Count == 0
            ? "Заказов пока нет"
            : "Нет заказов с таким статусом";
    }

    // ── Lifecycle actions (advance / cancel) ────────────────────────────────────────

    [RelayCommand]
    private async Task AdvanceAsync(OrderRowViewModel? row)
    {
        if (row is null || !row.ShowAdvance)
            return;

        Error = null;
        var result = await _orders.ChangeStatusAsync(row.Id, row.NextStatus);
        if (!result.Succeeded)
        {
            Error = result.Error;
            return;
        }

        await LoadAsync();
    }

    [RelayCommand]
    private async Task CancelOrderAsync(OrderRowViewModel? row)
    {
        if (row is null || !row.ShowCancel)
            return;

        Error = null;
        var result = await _orders.ChangeStatusAsync(row.Id, OrderStatus.Cancelled);
        if (!result.Succeeded)
        {
            Error = result.Error;
            return;
        }

        await LoadAsync();
    }

    // ── Export ──────────────────────────────────────────────────────────────────────

    [RelayCommand]
    private void Export()
    {
        Error = null;

        var dialog = new Microsoft.Win32.SaveFileDialog
        {
            Title = "Экспорт заказов",
            Filter = "CSV (разделитель — точка с запятой)|*.csv",
            // Include the time down to the second so repeated exports get unique, easy-to-save names.
            FileName = $"orders_{DateTime.Now:yyyy-MM-dd_HH-mm-ss}.csv"
        };
        if (dialog.ShowDialog() != true)
            return;

        try
        {
            var sb = new StringBuilder();
            sb.AppendLine("№;Покупатель;Дата;Позиции;Сумма;Статус");
            foreach (var o in Orders)
                sb.AppendLine(string.Join(';',
                    o.Id,
                    Csv(o.CustomerName),
                    o.CreatedAt.ToString("dd.MM.yyyy", CultureInfo.InvariantCulture),
                    o.ItemCount,
                    o.Total.ToString("0.00", CultureInfo.InvariantCulture),
                    o.StatusLabel));

            // UTF-8 with BOM so Excel opens the Cyrillic columns correctly.
            File.WriteAllText(dialog.FileName, sb.ToString(), new UTF8Encoding(true));
        }
        catch (Exception ex)
        {
            Error = $"Не удалось выполнить экспорт: {ex.Message}";
        }
    }

    /// <summary>Escape a CSV field: wrap in quotes if it contains the separator, a quote or a newline.</summary>
    private static string Csv(string value)
    {
        if (value.IndexOfAny(new[] { ';', '"', '\n', '\r' }) < 0)
            return value;
        return $"\"{value.Replace("\"", "\"\"")}\"";
    }
}

/// <summary>A status filter option: its caption and the status it selects (null = all statuses).</summary>
public sealed record StatusFilterOption(string Label, OrderStatus? Status);

/// <summary>
/// One row of the orders table. Wraps the <see cref="OrderRow"/> read model and folds in the role-based
/// capabilities for that row: whether to offer the next lifecycle step (<see cref="ShowAdvance"/>) or a
/// cancel (<see cref="ShowCancel"/>), so the view stays free of role logic.
/// </summary>
public sealed class OrderRowViewModel
{
    public OrderRowViewModel(OrderRow order, bool canManage, bool canShip)
    {
        _order = order;

        (NextStatus, AdvanceLabel) = order.Status switch
        {
            OrderStatus.New      => (OrderStatus.Reserved, "Резервировать"),
            OrderStatus.Reserved => (OrderStatus.Paid, "Оплатить"),
            OrderStatus.Paid     => (OrderStatus.Shipped, "Отгрузить"),
            OrderStatus.Shipped  => (OrderStatus.Completed, "Завершить"),
            _                    => (order.Status, null),
        };

        var hasNext = AdvanceLabel is not null;

        // Admin advances every step; a keeper only ships a paid order; a customer does neither.
        ShowAdvance = hasNext && (canManage || (canShip && order.Status == OrderStatus.Paid));
        ShowCancel = canManage && order.Status is OrderStatus.New or OrderStatus.Reserved or OrderStatus.Paid;
    }

    private readonly OrderRow _order;

    public int Id => _order.Id;
    public string CustomerName => _order.CustomerName;
    public DateTime CreatedAt => _order.CreatedAt;
    public int ItemCount => _order.ItemCount;
    public decimal Total => _order.Total;
    public OrderStatus Status => _order.Status;

    public OrderStatus NextStatus { get; }
    public string? AdvanceLabel { get; }
    public bool ShowAdvance { get; }
    public bool ShowCancel { get; }

    /// <summary>Russian caption of the current status, for the CSV export.</summary>
    public string StatusLabel => Status switch
    {
        OrderStatus.New => "Новая",
        OrderStatus.Reserved => "Зарезервирована",
        OrderStatus.Paid => "Оплачена",
        OrderStatus.Shipped => "Отгружена",
        OrderStatus.Completed => "Завершена",
        OrderStatus.Cancelled => "Отменена",
        _ => Status.ToString()
    };
}