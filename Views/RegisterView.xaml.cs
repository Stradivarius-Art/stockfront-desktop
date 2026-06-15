using System.Windows;
using System.Windows.Controls;
using stockfront.ViewModels;

namespace stockfront.Views;

/// <summary>
/// The registration window. Code-behind only bridges the two <see cref="PasswordBox"/>es to the
/// ViewModel and closes the dialog on success or cancel. <see cref="RegisteredEmail"/> lets the
/// host prefill the login afterwards.
/// </summary>
public partial class RegisterView : Window
{
    private readonly RegisterViewModel _viewModel;

    /// <summary>The e-mail created on success, or <c>null</c> if the user cancelled.</summary>
    public string? RegisteredEmail { get; private set; }

    public RegisterView(RegisterViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
        DataContext = viewModel;

        _viewModel.Registered += OnRegistered;
        _viewModel.BackRequested += OnBackRequested;
        Loaded += (_, _) => DisplayNameBox.Focus();
    }

    private void OnRegistered(string email)
    {
        RegisteredEmail = email;
        DialogResult = true;
        Close();
    }

    private void OnBackRequested()
    {
        DialogResult = false;
        Close();
    }

    private void PasswordBox_OnPasswordChanged(object sender, RoutedEventArgs e) =>
        _viewModel.Password = ((PasswordBox)sender).Password;

    private void ConfirmPasswordBox_OnPasswordChanged(object sender, RoutedEventArgs e) =>
        _viewModel.ConfirmPassword = ((PasswordBox)sender).Password;

    protected override void OnClosed(EventArgs e)
    {
        _viewModel.Registered -= OnRegistered;
        _viewModel.BackRequested -= OnBackRequested;
        base.OnClosed(e);
    }
}
