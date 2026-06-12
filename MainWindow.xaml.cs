using System.Windows;
using stockfront.ViewModels;

namespace stockfront;

/// <summary>
/// The main shell shown after a successful sign-in. Code-behind only forwards the ViewModel's
/// logout request up to the host, which returns to the login window.
/// </summary>
public partial class MainWindow : Window
{
    private readonly MainViewModel _viewModel;

    /// <summary>Raised when the signed-in user asks to log out.</summary>
    public event EventHandler? LogoutRequested;

    public MainWindow(MainViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
        DataContext = viewModel;

        _viewModel.LogoutRequested += OnLogoutRequested;
    }

    private void OnLogoutRequested() => LogoutRequested?.Invoke(this, EventArgs.Empty);

    protected override void OnClosed(EventArgs e)
    {
        _viewModel.LogoutRequested -= OnLogoutRequested;
        base.OnClosed(e);
    }
}