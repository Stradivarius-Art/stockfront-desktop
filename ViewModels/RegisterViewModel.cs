using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using StockFront.Contracts.Auth;

namespace stockfront.ViewModels;

/// <summary>
/// Drives the registration window. Validates each field with the shared <see cref="CredentialRules"/>
/// (live as the user types and again on submit), then calls <see cref="IAuthService.RegisterAsync"/>.
/// New accounts get the <see cref="UserRole.Customer"/> role.
/// </summary>
public sealed partial class RegisterViewModel : ObservableObject
{
    private readonly IAuthService _auth;

    public RegisterViewModel(IAuthService auth) => _auth = auth;

    /// <summary>Raised on success with the new e-mail, so the host can return to the login screen.</summary>
    public event Action<string>? Registered;

    /// <summary>Raised when the user cancels and wants to go back to login.</summary>
    public event Action? BackRequested;

    // --- Bound text fields (validated live via the generated On<Field>Changed hooks) ---

    [ObservableProperty] private string _email = string.Empty;
    [ObservableProperty] private string _displayName = string.Empty;

    // --- Passwords come from PasswordBoxes via code-behind (not bindable) ---

    private string _password = string.Empty;
    public string Password
    {
        private get => _password;
        set { _password = value; PasswordError = CredentialRules.ValidatePassword(value); RevalidateConfirmation(); }
    }

    private string _confirmPassword = string.Empty;
    public string ConfirmPassword
    {
        private get => _confirmPassword;
        set { _confirmPassword = value; RevalidateConfirmation(); }
    }

    // --- Per-field error messages (null = valid) ---

    [ObservableProperty] private string? _emailError;
    [ObservableProperty] private string? _displayNameError;
    [ObservableProperty] private string? _passwordError;
    [ObservableProperty] private string? _confirmPasswordError;

    /// <summary>A general error not tied to one field (e.g. a database failure).</summary>
    [ObservableProperty] private string? _formError;

    [ObservableProperty] private bool _isBusy;

    partial void OnEmailChanged(string value) => EmailError = CredentialRules.ValidateEmail(value);
    partial void OnDisplayNameChanged(string value) => DisplayNameError = CredentialRules.ValidateDisplayName(value);

    private void RevalidateConfirmation() =>
        ConfirmPasswordError = CredentialRules.ValidatePasswordConfirmation(_password, _confirmPassword);

    private bool Validate()
    {
        EmailError = CredentialRules.ValidateEmail(Email);
        DisplayNameError = CredentialRules.ValidateDisplayName(DisplayName);
        PasswordError = CredentialRules.ValidatePassword(_password);
        ConfirmPasswordError = CredentialRules.ValidatePasswordConfirmation(_password, _confirmPassword);

        return EmailError is null && DisplayNameError is null
               && PasswordError is null && ConfirmPasswordError is null;
    }

    [RelayCommand]
    private async Task RegisterAsync()
    {
        FormError = null;
        if (!Validate())
            return;

        IsBusy = true;
        try
        {
            var result = await _auth.RegisterAsync(Email, _password, DisplayName);
            if (result.Succeeded)
            {
                Registered?.Invoke(result.User!.Email);
                return;
            }

            // Map the server-side uniqueness error back onto the relevant field.
            if (result.Error?.Contains("mail", StringComparison.OrdinalIgnoreCase) == true)
                EmailError = result.Error;
            else
                FormError = result.Error;
        }
        catch (Exception ex)
        {
            FormError = $"Ошибка регистрации: {ex.Message}";
        }
        finally
        {
            _password = string.Empty;
            _confirmPassword = string.Empty;
            IsBusy = false;
        }
    }

    [RelayCommand]
    private void Back() => BackRequested?.Invoke();
}
