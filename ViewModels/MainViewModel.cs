using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using StockFront.Contracts.Auth;

namespace stockfront.ViewModels;

/// <summary>
/// ViewModel for the main shell (top bar + sidebar + content area). Shows who is signed in, hosts
/// the current page in <see cref="CurrentPage"/>, and switches between the dashboard and the
/// not-yet-built sections.
/// </summary>
public sealed partial class MainViewModel : ObservableObject
{
    private readonly ICurrentUser _currentUser;
    private readonly IAuthService _auth;
    private readonly DashboardViewModel _dashboard;

    public MainViewModel(ICurrentUser currentUser, IAuthService auth, DashboardViewModel dashboard)
    {
        _currentUser = currentUser;
        _auth = auth;
        _dashboard = dashboard;
    }

    /// <summary>Raised after the user signs out, so the host can return to the login window.</summary>
    public event Action? LogoutRequested;

    /// <summary>The page currently shown in the content area (a page ViewModel).</summary>
    [ObservableProperty] private object? _currentPage;

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

    /// <summary>Open the dashboard and load it. Called once when the shell appears.</summary>
    public Task InitializeAsync() => NavigateAsync("dashboard");

    [RelayCommand]
    private async Task NavigateAsync(string section)
    {
        switch (section)
        {
            case "dashboard":
                CurrentPage = _dashboard;
                await _dashboard.LoadAsync();
                break;
            case "warehouse":
                CurrentPage = new PlaceholderViewModel("Склад");
                break;
            case "storefront":
                CurrentPage = new PlaceholderViewModel("Витрина");
                break;
            case "orders":
                CurrentPage = new PlaceholderViewModel("Заказы");
                break;
            case "reports":
                CurrentPage = new PlaceholderViewModel("Отчёты");
                break;
        }
    }

    [RelayCommand]
    private void Logout()
    {
        _auth.Logout();
        LogoutRequested?.Invoke();
    }
}
