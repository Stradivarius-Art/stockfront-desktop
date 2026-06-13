using System.Windows;
using Microsoft.Extensions.DependencyInjection;
using StockFront.Auth;
using StockFront.Data;
using stockfront.Infrastructure;
using stockfront.ViewModels;
using stockfront.Views;

namespace stockfront;

/// <summary>
/// Integration host. Builds the DI container, brings the database up to date, then shows the
/// login window; the main window opens only after a successful sign-in.
/// </summary>
public partial class App : Application
{
    private ServiceProvider _services = null!;

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        // The login dialog isn't the "main window", so don't let the app exit when it closes —
        // we decide explicitly whether to go on to the main window or quit.
        ShutdownMode = ShutdownMode.OnExplicitShutdown;

        // Connection string comes from the .env file / environment, never from code (see DotEnv).
        DotEnv.Load();
        var connectionString = Environment.GetEnvironmentVariable(AppDbContextFactory.ConnectionEnvVar);
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            MessageBox.Show(
                $"Не задана строка подключения к базе данных. Укажите переменную " +
                $"'{AppDbContextFactory.ConnectionEnvVar}' или создайте файл .env (см. .env.example).",
                "StockFront", MessageBoxButton.OK, MessageBoxImage.Error);
            Shutdown(1);
            return;
        }

        _services = BuildServiceProvider(connectionString);

        try
        {
            // Apply migrations and seed the default admin if the users table is empty.
            // Run on the thread pool (not via a bare GetResult on the UI thread): the async DB work
            // captures no UI SynchronizationContext there, so blocking the UI thread can't deadlock.
            Task.Run(() => DatabaseBootstrapper.InitializeAsync(_services)).GetAwaiter().GetResult();
        }
        catch (Exception ex)
        {
            MessageBox.Show(
                $"Не удалось подготовить базу данных:\n{ex.Message}",
                "StockFront", MessageBoxButton.OK, MessageBoxImage.Error);
            Shutdown(1);
            return;
        }

        ShowWelcomeFlow();
    }

    /// <summary>
    /// Show the welcome window first. From it the user opens the login or registration dialog; a
    /// successful sign-in (set via <see cref="Window.DialogResult"/>) closes the welcome window and
    /// opens the main window. Closing the welcome window without signing in quits the app.
    /// </summary>
    private void ShowWelcomeFlow()
    {
        var welcome = _services.GetRequiredService<WelcomeView>();

        welcome.LoginRequested += (_, _) =>
        {
            if (RunLoginDialog(welcome))
                welcome.DialogResult = true;
        };
        welcome.RegisterRequested += (_, _) =>
        {
            // Register, then drop straight into login with the new username prefilled.
            if (RunRegisterDialog(welcome) is { } username && RunLoginDialog(welcome, username))
                welcome.DialogResult = true;
        };

        if (welcome.ShowDialog() == true)
            OpenMainWindow();
        else
            Shutdown();
    }

    /// <summary>
    /// Show the login dialog owned by <paramref name="owner"/> (null for a standalone window).
    /// Keeps the login↔register cross-link working and optionally prefills a username. Returns
    /// <c>true</c> once the user has signed in.
    /// </summary>
    private bool RunLoginDialog(Window? owner, string? prefillUsername = null)
    {
        var login = _services.GetRequiredService<LoginView>();
        login.Owner = owner;
        if (prefillUsername is not null)
            login.NotifyRegistered(prefillUsername);

        login.RegisterRequested += OnRegisterFromLogin;
        var signedIn = login.ShowDialog() == true;
        login.RegisterRequested -= OnRegisterFromLogin;
        return signedIn;
    }

    /// <summary>
    /// Open the registration dialog on top of the (still-open) login dialog. On success, return to
    /// login with the new username prefilled so the user can sign in.
    /// </summary>
    private void OnRegisterFromLogin(object? sender, EventArgs e)
    {
        var login = (LoginView)sender!;
        if (RunRegisterDialog(login) is { } username)
            login.NotifyRegistered(username);
    }

    /// <summary>Show the registration dialog; returns the new username, or null if cancelled.</summary>
    private string? RunRegisterDialog(Window owner)
    {
        var register = _services.GetRequiredService<RegisterView>();
        register.Owner = owner;
        return register.ShowDialog() == true ? register.RegisteredUsername : null;
    }

    private void OpenMainWindow()
    {
        var main = _services.GetRequiredService<MainWindow>();
        main.LogoutRequested += OnLogoutRequested;
        MainWindow = main;
        ShutdownMode = ShutdownMode.OnMainWindowClose;
        main.Show();
    }

    private void OnLogoutRequested(object? sender, EventArgs e)
    {
        var main = (MainWindow)sender!;
        main.LogoutRequested -= OnLogoutRequested;

        // Don't let closing the main window quit the app — we're returning to the login screen.
        ShutdownMode = ShutdownMode.OnExplicitShutdown;
        main.Close();

        // Logging out returns straight to the login form (not the welcome window).
        if (RunLoginDialog(owner: null))
            OpenMainWindow();
        else
            Shutdown();
    }

    private static ServiceProvider BuildServiceProvider(string connectionString)
    {
        var services = new ServiceCollection();

        services.AddDataLayer(connectionString);   // AppDbContext + repositories
        services.AddAuthModule();                   // IAuthService, ICurrentUser, hasher

        services.AddTransient<WelcomeViewModel>();
        services.AddTransient<WelcomeView>();
        services.AddTransient<LoginViewModel>();
        services.AddTransient<LoginView>();
        services.AddTransient<RegisterViewModel>();
        services.AddTransient<RegisterView>();
        services.AddTransient<DashboardViewModel>();
        services.AddTransient<MainViewModel>();
        services.AddTransient<MainWindow>();

        return services.BuildServiceProvider();
    }

    protected override void OnExit(ExitEventArgs e)
    {
        _services?.Dispose();
        base.OnExit(e);
    }
}
