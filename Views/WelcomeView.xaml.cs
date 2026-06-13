using System.Windows;
using stockfront.ViewModels;

namespace stockfront.Views;

/// <summary>
/// The welcome / landing window shown on startup. Code-behind only re-raises the ViewModel's two
/// intents as window-level events so the host can open the login or registration dialog on top.
/// </summary>
public partial class WelcomeView : Window
{
    private readonly WelcomeViewModel _viewModel;

    /// <summary>Raised when the user chooses to sign in.</summary>
    public event EventHandler? LoginRequested;

    /// <summary>Raised when the user chooses to register.</summary>
    public event EventHandler? RegisterRequested;

    public WelcomeView(WelcomeViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
        DataContext = viewModel;

        _viewModel.LoginRequested += OnLoginRequested;
        _viewModel.RegisterRequested += OnRegisterRequested;
    }

    private void OnLoginRequested() => LoginRequested?.Invoke(this, EventArgs.Empty);

    private void OnRegisterRequested() => RegisterRequested?.Invoke(this, EventArgs.Empty);

    protected override void OnClosed(EventArgs e)
    {
        _viewModel.LoginRequested -= OnLoginRequested;
        _viewModel.RegisterRequested -= OnRegisterRequested;
        base.OnClosed(e);
    }
}
