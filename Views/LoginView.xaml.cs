using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
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
        Loaded += (_, _) => EmailBox.Focus();
    }

    /// <summary>Prefill the e-mail after a successful registration (called by the host).</summary>
    public void NotifyRegistered(string email) => _viewModel.NotifyRegistered(email);

    private void OnLoginSucceeded()
    {
        DialogResult = true;   // ShowDialog() returns true → App opens the main window
        Close();
    }

    private void OnRegisterRequested() => RegisterRequested?.Invoke(this, EventArgs.Empty);

    private void PasswordBox_OnPasswordChanged(object sender, RoutedEventArgs e) =>
        _viewModel.Password = ((PasswordBox)sender).Password;

    // Hold-to-reveal: while the eye button is held down, show the plain password in an overlay
    // TextBox; restore the masked PasswordBox on release or when the cursor leaves the button.
    private void EyeButton_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e) => RevealPassword();
    private void EyeButton_PreviewMouseLeftButtonUp(object sender, MouseButtonEventArgs e) => HidePassword();
    private void EyeButton_MouseLeave(object sender, MouseEventArgs e) => HidePassword();

    private void RevealPassword()
    {
        PasswordTextBox.Text = PasswordBox.Password;
        PasswordTextBox.Visibility = Visibility.Visible;
        PasswordBox.Visibility = Visibility.Collapsed;
        EyeIcon.Kind = MahApps.Metro.IconPacks.PackIconMaterialKind.EyeOffOutline;
    }

    private void HidePassword()
    {
        PasswordTextBox.Visibility = Visibility.Collapsed;
        PasswordBox.Visibility = Visibility.Visible;
        EyeIcon.Kind = MahApps.Metro.IconPacks.PackIconMaterialKind.EyeOutline;
    }

    protected override void OnClosed(EventArgs e)
    {
        _viewModel.LoginSucceeded -= OnLoginSucceeded;
        _viewModel.RegisterRequested -= OnRegisterRequested;
        base.OnClosed(e);
    }
}
