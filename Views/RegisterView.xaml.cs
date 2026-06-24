using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using MahApps.Metro.IconPacks;
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

    // The space bar would otherwise enter a space into the password — disallowed by CredentialRules,
    // so block it at the source (the field also rejects other whitespace/non-ASCII on validation).
    private void PasswordBox_BlockWhitespace(object sender, KeyEventArgs e)
    {
        if (e.Key is Key.Space)
            e.Handled = true;
    }

    private void PasswordBox_OnPasswordChanged(object sender, RoutedEventArgs e) =>
        _viewModel.Password = ((PasswordBox)sender).Password;

    private void ConfirmPasswordBox_OnPasswordChanged(object sender, RoutedEventArgs e) =>
        _viewModel.ConfirmPassword = ((PasswordBox)sender).Password;

    // Hold-to-reveal: while an eye button is held, show the plain password in its overlay TextBox;
    // restore the masked PasswordBox on release or when the cursor leaves the button. The button's
    // Tag ("Password"/"Confirm") selects which field to act on.
    private void EyeButton_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e) =>
        SetRevealed(sender, true);
    private void EyeButton_PreviewMouseLeftButtonUp(object sender, MouseButtonEventArgs e) =>
        SetRevealed(sender, false);
    private void EyeButton_MouseLeave(object sender, MouseEventArgs e) =>
        SetRevealed(sender, false);

    private void SetRevealed(object sender, bool revealed)
    {
        var isConfirm = (string?)((FrameworkElement)sender).Tag == "Confirm";
        var passwordBox = isConfirm ? ConfirmPasswordBox : PasswordBox;
        var textBox = isConfirm ? ConfirmPasswordTextBox : PasswordTextBox;
        var icon = isConfirm ? ConfirmPasswordEyeIcon : PasswordEyeIcon;

        if (revealed)
            textBox.Text = passwordBox.Password;
        textBox.Visibility = revealed ? Visibility.Visible : Visibility.Collapsed;
        passwordBox.Visibility = revealed ? Visibility.Collapsed : Visibility.Visible;
        icon.Kind = revealed ? PackIconMaterialKind.EyeOffOutline : PackIconMaterialKind.EyeOutline;
    }

    protected override void OnClosed(EventArgs e)
    {
        _viewModel.Registered -= OnRegistered;
        _viewModel.BackRequested -= OnBackRequested;
        base.OnClosed(e);
    }
}
