using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using StockFront.Contracts.Auth;

namespace stockfront.ViewModels;

/// <summary>
/// ViewModel for the main shell (top bar + sidebar + content area). Shows who is signed in, hosts
/// the current page in <see cref="CurrentPage"/>, and gates which sections the signed-in role may
/// open. The role → section matrix lives in <see cref="CanAccess"/> (see CLAUDE.md "Роли и доступ").
/// </summary>
public sealed partial class MainViewModel : ObservableObject
{
    private readonly ICurrentUser _currentUser;
    private readonly IAuthService _auth;
    private readonly DashboardViewModel _dashboard;
    private readonly WarehouseViewModel _warehouse;
    private readonly StorefrontViewModel _storefront;
    private readonly OrdersViewModel _orders;

    public MainViewModel(
        ICurrentUser currentUser, IAuthService auth, DashboardViewModel dashboard,
        WarehouseViewModel warehouse, StorefrontViewModel storefront, OrdersViewModel orders)
    {
        _currentUser = currentUser;
        _auth = auth;
        _dashboard = dashboard;
        _warehouse = warehouse;
        _storefront = storefront;
        _orders = orders;
    }

    /// <summary>Raised after the user signs out, so the host can return to the login window.</summary>
    public event Action? LogoutRequested;

    /// <summary>The page currently shown in the content area (a page ViewModel).</summary>
    [ObservableProperty] private object? _currentPage;

    /// <summary>The open section ("dashboard", "warehouse", …); drives the sidebar highlight.</summary>
    [ObservableProperty] private string _currentSection = "dashboard";

    public string DisplayName => _currentUser.User?.DisplayName ?? "Гость";

    public string RoleText => _currentUser.User?.Role switch
    {
        UserRole.Admin => "Администратор",
        UserRole.WarehouseKeeper => "Кладовщик",
        UserRole.Customer => "Покупатель",
        _ => "—"
    };

    /// <summary>Initials for the avatar chip, e.g. "Кузнецов Дмитрий" → "КД".</summary>
    public string Initials
    {
        get
        {
            var name = _currentUser.User?.DisplayName;
            if (string.IsNullOrWhiteSpace(name))
                return "?";

            var parts = name.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            var first = parts[0][0];
            var second = parts.Length > 1 ? parts[1][0] : (parts[0].Length > 1 ? parts[0][1] : ' ');
            return $"{char.ToUpper(first)}{char.ToUpper(second)}".Trim();
        }
    }

    private UserRole? Role => _currentUser.User?.Role;

    // Sidebar visibility per role — bound to each menu item's Visibility (see the access matrix in CLAUDE.md).
    public bool ShowDashboard => CanAccess("dashboard");
    public bool ShowWarehouse => CanAccess("warehouse");
    public bool ShowStorefront => CanAccess("storefront");
    public bool ShowOrders => CanAccess("orders");

    /// <summary>
    /// Role gate for a section. Enforced here, not only hidden in XAML (see the stockfront-auth
    /// skill): a forbidden section can't be opened even if its command is triggered directly.
    /// </summary>
    private bool CanAccess(string section) => Role switch
    {
        UserRole.Admin => true,
        UserRole.WarehouseKeeper =>
            section is "dashboard" or "warehouse" or "orders" or "profile",
        UserRole.Customer =>
            section is "storefront" or "orders" or "profile",
        _ => false
    };

    /// <summary>
    /// Open the start screen for the current role. Customers have no dashboard, so they land straight
    /// on the storefront; everyone else opens the dashboard. Called once when the shell appears.
    /// </summary>
    public Task InitializeAsync() =>
        NavigateAsync(Role == UserRole.Customer ? "storefront" : "dashboard");

    [RelayCommand]
    private async Task NavigateAsync(string section)
    {
        if (!CanAccess(section))
            return;

        CurrentSection = section;

        if (section == "dashboard")
        {
            CurrentPage = _dashboard;
            await _dashboard.LoadAsync();
        }
        else if (section == "warehouse")
        {
            CurrentPage = _warehouse;
            await _warehouse.LoadAsync();
        }
        else if (section == "storefront")
        {
            CurrentPage = _storefront;
            await _storefront.LoadAsync();
        }
        else if (section == "orders")
        {
            CurrentPage = _orders;
            await _orders.LoadAsync();
        }
        else
        {
            CurrentPage = BuildPlaceholder(section);
        }
    }

    /// <summary>
    /// Build the stub page for a not-yet-implemented section, with a description tailored to what the
    /// current role is allowed to do there. TODO: replace these stubs with the real module views and
    /// move the in-screen capability rules onto explicit permission flags (see CLAUDE.md).
    /// </summary>
    private PlaceholderViewModel BuildPlaceholder(string section)
    {
        var (title, description) = (section, Role) switch
        {
            ("storefront", UserRole.Admin) =>
                ("Витрина", "Каталог, корзина, оформление и редактирование карточек товаров."),
            ("storefront", _) =>
                ("Витрина", "Каталог товаров и корзина."),

            ("profile", _) =>
                ("Профиль", "Личные данные и смена пароля."),

            _ => (section, "Раздел в разработке."),
        };

        return new PlaceholderViewModel(title, description);
    }

    [RelayCommand]
    private void Logout()
    {
        _auth.Logout();
        LogoutRequested?.Invoke();
    }
}