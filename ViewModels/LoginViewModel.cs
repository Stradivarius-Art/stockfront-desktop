using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using StockFront.Contracts.Auth;

namespace stockfront.ViewModels;

/// <summary>
/// Drives the login window. Holds the entered credentials, calls <see cref="IAuthService"/>, and
/// raises <see cref="LoginSucceeded"/> so the view can close and the app can open the main window.
/// Login validation is deliberately minimal — the error stays generic so we never reveal which
/// part was wrong (field-level rules belong on the registration screen, not here).
/// </summary>
public sealed partial class LoginViewModel : ObservableObject
{
    private readonly IAuthService _auth;

    public LoginViewModel(IAuthService auth) => _auth = auth;

    /// <summary>Raised once after a successful sign-in.</summary>
    public event Action? LoginSucceeded;

    /// <summary>Raised when the user wants to open the registration screen.</summary>
    public event Action? RegisterRequested;

    [ObservableProperty]
    private string _email = string.Empty;

    private string _password = string.Empty;

    /// <summary>
    /// Set from the view's <c>PasswordBox</c> (which can't be data-bound). Kept as a plain field,
    /// never persisted, and cleared after a sign-in attempt.
    /// </summary>
    public string Password
    {
        private get => _password;
        set => _password = value;
    }

    [ObservableProperty]
    private string? _errorMessage;

    /// <summary>Non-error notice, e.g. "регистрация успешна, войдите".</summary>
    [ObservableProperty]
    private string? _infoMessage;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(LoginCommand))]
    private bool _isBusy;

    /// <summary>Prefill the e-mail after a successful registration and invite the user to sign in.</summary>
    public void NotifyRegistered(string email)
    {
        Email = email;
        ErrorMessage = null;
        InfoMessage = "Регистрация успешна. Войдите под новым e-mail.";
    }

    // The button stays enabled even with empty fields (we don't lock the user out before they type);
    // it's only disabled mid-request to prevent a double submit. Empty fields surface as a validation
    // message on submit instead.
    private bool CanLogin() => !IsBusy;

    [RelayCommand(CanExecute = nameof(CanLogin))]
    private async Task LoginAsync()
    {
        ErrorMessage = null;
        InfoMessage = null;

        if (string.IsNullOrWhiteSpace(Email) || _password.Length == 0)
        {
            ErrorMessage = "Введите e-mail и пароль.";
            return;
        }

        IsBusy = true;
        try
        {
            var result = await _auth.LoginAsync(Email, _password);
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

    [RelayCommand]
    private void GoToRegister() => RegisterRequested?.Invoke();
}
