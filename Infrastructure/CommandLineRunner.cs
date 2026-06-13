using System.Diagnostics;
using System.Runtime.InteropServices;

namespace stockfront.Infrastructure;

/// <summary>
/// Handles database-maintenance commands passed on the command line, e.g.
/// <c>dotnet run --project stockfront.csproj -- --migrate --seed-admin --seed</c>.
/// These run instead of the GUI: the app does the requested work, prints the result to the
/// terminal, and exits. If no known flag is present, <see cref="TryRunAsync"/> returns
/// <c>false</c> and the app starts normally without touching the database.
/// </summary>
public static class CommandLineRunner
{
    private const string MigrateFlag = "--migrate";
    private const string SeedAdminFlag = "--seed-admin";
    private const string SeedFlag = "--seed";

    /// <summary>
    /// Run any recognised maintenance commands found in <paramref name="args"/>. Returns
    /// <c>true</c> if the process ran in command-line mode (and the app should exit afterwards),
    /// or <c>false</c> for a normal GUI launch.
    /// </summary>
    public static async Task<bool> TryRunAsync(IServiceProvider services, string[] args)
    {
        var migrate = args.Contains(MigrateFlag);
        var seedAdmin = args.Contains(SeedAdminFlag);
        var seed = args.Contains(SeedFlag);

        if (!migrate && !seedAdmin && !seed)
            return false;

        AttachConsole();

        // Fixed order regardless of how the flags were passed: schema → admin → demo data.
        if (migrate)
        {
            Write("Применение миграций…");
            await DatabaseBootstrapper.MigrateAsync(services);
            Write("Миграции применены.");
        }

        if (seedAdmin)
        {
            Write("Создание администратора по умолчанию…");
            await DatabaseBootstrapper.SeedAdminAsync(services);
            Write("Готово (если таблица пользователей была пуста).");
        }

        if (seed)
        {
            Write("Заполнение демо-данными…");
            await DatabaseBootstrapper.SeedTestDataAsync(services);
            Write("Готово (если товаров в базе ещё не было).");
        }

        return true;
    }

    private static void Write(string message)
    {
        Console.WriteLine($"[StockFront] {message}");
        Debug.WriteLine(message);
    }

    // The host is a WPF app (WinExe) without its own console window. On Windows, attach to the
    // parent terminal's console so Console.WriteLine is visible when launched from a shell.
    private static void AttachConsole()
    {
        if (!OperatingSystem.IsWindows())
            return;

        try
        {
            AttachConsole(AttachParentProcess);
        }
        catch (DllNotFoundException)
        {
            // No console to attach to — Debug.WriteLine still works for diagnostics.
        }
    }

    private const int AttachParentProcess = -1;

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern bool AttachConsole(int dwProcessId);
}