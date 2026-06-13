using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace stockfront.ViewModels;

/// <summary>
/// Drives the welcome / landing window — the first screen shown on startup. It offers the two
/// entry points into the app (sign in, register) and raises an event for each so the host can
/// open the matching dialog. It holds no state of its own.
/// </summary>
public sealed partial class WelcomeViewModel : ObservableObject
{
    /// <summary>Raised when the user wants to open the login screen.</summary>
    public event Action? LoginRequested;

    /// <summary>Raised when the user wants to open the registration screen.</summary>
    public event Action? RegisterRequested;

    [RelayCommand]
    private void Login() => LoginRequested?.Invoke();

    [RelayCommand]
    private void Register() => RegisterRequested?.Invoke();
}
