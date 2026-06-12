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
            DatabaseBootstrapper.InitializeAsync(_services).GetAwaiter().GetResult();
        }
        catch (Exception ex)
        {
            MessageBox.Show(
                $"Не удалось подготовить базу данных:\n{ex.Message}",
                "StockFront", MessageBoxButton.OK, MessageBoxImage.Error);
            Shutdown(1);
            return;
        }

        ShowLoginFlow();
    }

    /// <summary>
    /// Show the login dialog; on success open the main window, on cancel quit. Logging out from the
    /// main window brings the user back here.
    /// </summary>
    private void ShowLoginFlow()
    {
        var login = _services.GetRequiredService<LoginView>();
        if (login.ShowDialog() != true)
        {
            Shutdown();
            return;
        }

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

        ShowLoginFlow();
    }

    private static ServiceProvider BuildServiceProvider(string connectionString)
    {
        var services = new ServiceCollection();

        services.AddDataLayer(connectionString);   // AppDbContext + repositories
        services.AddAuthModule();                   // IAuthService, ICurrentUser, hasher

        services.AddTransient<LoginViewModel>();
        services.AddTransient<LoginView>();
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
