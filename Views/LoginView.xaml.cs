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

    public LoginView(LoginViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
        DataContext = viewModel;

        _viewModel.LoginSucceeded += OnLoginSucceeded;
        Loaded += (_, _) => UsernameBox.Focus();
    }

    private void OnLoginSucceeded()
    {
        DialogResult = true;   // ShowDialog() returns true → App opens the main window
        Close();
    }

    private void PasswordBox_OnPasswordChanged(object sender, RoutedEventArgs e) =>
        _viewModel.Password = ((PasswordBox)sender).Password;

    protected override void OnClosed(EventArgs e)
    {
        _viewModel.LoginSucceeded -= OnLoginSucceeded;
        base.OnClosed(e);
    }
}
