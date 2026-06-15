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

        // Database maintenance commands (--migrate / --seed-admin / --seed) run instead of the GUI.
        // A normal launch never migrates or seeds — the schema is prepared via these terminal
        // commands, so starting the app can't clobber existing data. Run the async DB work on the
        // thread pool (not a bare GetResult on the UI thread): it captures no UI SynchronizationContext
        // there, so blocking the UI thread can't deadlock.
        try
        {
            var handled = Task.Run(() => CommandLineRunner.TryRunAsync(_services, e.Args))
                .GetAwaiter().GetResult();
            if (handled)
            {
                Shutdown(0);
                return;
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show(
                $"Не удалось выполнить команду обслуживания базы данных:\n{ex.Message}",
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
            // Register, then drop straight into login with the new e-mail prefilled.
            if (RunRegisterDialog(welcome) is { } email && RunLoginDialog(welcome, email))
                welcome.DialogResult = true;
        };

        if (welcome.ShowDialog() == true)
            OpenMainWindow();
        else
            Shutdown();
    }

    /// <summary>
    /// Show the login dialog owned by <paramref name="owner"/> (null for a standalone window).
    /// Keeps the login↔register cross-link working and optionally prefills an e-mail. Returns
    /// <c>true</c> once the user has signed in.
    /// </summary>
    private bool RunLoginDialog(Window? owner, string? prefillEmail = null)
    {
        var login = _services.GetRequiredService<LoginView>();
        login.Owner = owner;
        if (prefillEmail is not null)
            login.NotifyRegistered(prefillEmail);

        login.RegisterRequested += OnRegisterFromLogin;
        var signedIn = login.ShowDialog() == true;
        login.RegisterRequested -= OnRegisterFromLogin;
        return signedIn;
    }

    /// <summary>
    /// Open the registration dialog on top of the (still-open) login dialog. On success, return to
    /// login with the new e-mail prefilled so the user can sign in.
    /// </summary>
    private void OnRegisterFromLogin(object? sender, EventArgs e)
    {
        var login = (LoginView)sender!;
        if (RunRegisterDialog(login) is { } email)
            login.NotifyRegistered(email);
    }

    /// <summary>Show the registration dialog; returns the new e-mail, or null if cancelled.</summary>
    private string? RunRegisterDialog(Window owner)
    {
        var register = _services.GetRequiredService<RegisterView>();
        register.Owner = owner;
        return register.ShowDialog() == true ? register.RegisteredEmail : null;
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
