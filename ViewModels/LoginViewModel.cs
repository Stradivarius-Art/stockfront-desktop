using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using StockFront.Contracts.Auth;

namespace stockfront.ViewModels;

/// <summary>
/// Drives the login window. Holds the entered credentials, calls <see cref="IAuthService"/>, and
/// raises <see cref="LoginSucceeded"/> so the view can close and the app can open the main window.
/// </summary>
public sealed partial class LoginViewModel : ObservableObject
{
    private readonly IAuthService _auth;

    public LoginViewModel(IAuthService auth) => _auth = auth;

    /// <summary>Raised once after a successful sign-in.</summary>
    public event Action? LoginSucceeded;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(LoginCommand))]
    private string _username = string.Empty;

    /// <summary>
    /// Set from the view's <c>PasswordBox</c> (which can't be data-bound). Kept as a plain field,
    /// never persisted, and cleared after a sign-in attempt.
    /// </summary>
    public string Password { private get; set; } = string.Empty;

    [ObservableProperty]
    private string? _errorMessage;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(LoginCommand))]
    private bool _isBusy;

    private bool CanLogin() => !IsBusy && !string.IsNullOrWhiteSpace(Username);

    [RelayCommand(CanExecute = nameof(CanLogin))]
    private async Task LoginAsync()
    {
        ErrorMessage = null;
        IsBusy = true;
        try
        {
            var result = await _auth.LoginAsync(Username, Password);
            if (result.Succeeded)
            {
                LoginSucceeded?.Invoke();
                return;
            }

            ErrorMessage = result.Error;
        }
        catch (Exception ex)
        {
            // A DB outage or bad connection string shouldn't crash the window.
            ErrorMessage = $"Ошибка входа: {ex.Message}";
        }
        finally
        {
            Password = string.Empty;
            IsBusy = false;
        }
    }
}
