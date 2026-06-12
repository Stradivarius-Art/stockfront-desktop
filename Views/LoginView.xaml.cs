using System.Windows;
using System.Windows.Controls;
using stockfront.ViewModels;

namespace stockfront.Views;

/// <summary>
/// The login window. Code-behind is limited to what XAML can't do: bridging the non-bindable
/// <see cref="PasswordBox"/> to the ViewModel and closing the dialog once sign-in succeeds.
/// </summary>
public partial class LoginView : Window
{
    private readonly LoginViewModel _viewModel;

    /// <summary>Raised when the user asks to open the registration screen.</summary>
    public event EventHandler? RegisterRequested;

    public LoginView(LoginViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
        DataContext = viewModel;

        _viewModel.LoginSucceeded += OnLoginSucceeded;
        _viewModel.RegisterRequested += OnRegisterRequested;
        Loaded += (_, _) => UsernameBox.Focus();
    }

    /// <summary>Prefill the login after a successful registration (called by the host).</summary>
    public void NotifyRegistered(string username) => _viewModel.NotifyRegistered(username);

    private void OnLoginSucceeded()
    {
        DialogResult = true;   // ShowDialog() returns true → App opens the main window
        Close();
    }

    private void OnRegisterRequested() => RegisterRequested?.Invoke(this, EventArgs.Empty);

    private void PasswordBox_OnPasswordChanged(object sender, RoutedEventArgs e) =>
        _viewModel.Password = ((PasswordBox)sender).Password;

    protected override void OnClosed(EventArgs e)
    {
        _viewModel.LoginSucceeded -= OnLoginSucceeded;
        _viewModel.RegisterRequested -= OnRegisterRequested;
        base.OnClosed(e);
    }
}
