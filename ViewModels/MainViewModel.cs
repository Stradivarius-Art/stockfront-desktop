using CommunityToolkit.Mvvm.Input;
using StockFront.Contracts.Auth;

namespace stockfront.ViewModels;

/// <summary>
/// ViewModel for the main shell. For now it only proves authorization works — it shows who is
/// signed in and lets them sign out. The full dashboard is built on top of this later.
/// </summary>
public sealed partial class MainViewModel
{
    private readonly ICurrentUser _currentUser;
    private readonly IAuthService _auth;

    public MainViewModel(ICurrentUser currentUser, IAuthService auth)
    {
        _currentUser = currentUser;
        _auth = auth;
    }

    /// <summary>Raised after the user signs out, so the host can return to the login window.</summary>
    public event Action? LogoutRequested;

    public string DisplayName => _currentUser.User?.DisplayName ?? "Гость";

    public string RoleText => _currentUser.User?.Role switch
    {
        UserRole.Admin => "Администратор",
        UserRole.WarehouseKeeper => "Кладовщик",
        UserRole.Customer => "Покупатель",
        _ => "—"
    };

    /// <summary>Initials for the avatar chip, e.g. "Кладовщик Логин" → "КЛ".</summary>
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

    [RelayCommand]
    private void Logout()
    {
        _auth.Logout();
        LogoutRequested?.Invoke();
    }
}
