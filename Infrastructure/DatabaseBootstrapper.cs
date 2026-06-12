using System.Diagnostics;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using StockFront.Contracts.Auth;
using StockFront.Contracts.Persistence;
using StockFront.Data;

namespace stockfront.Infrastructure;

/// <summary>
/// One-time startup work: bring the schema up to date and make sure there is at least one account
/// to sign in with. Runs before any window is shown.
/// </summary>
public static class DatabaseBootstrapper
{
    // Default administrator seeded only into an empty users table. MUST be changed after the first
    // sign-in; it exists so a fresh database isn't locked out of its own admin area.
    private const string DefaultAdminUsername = "admin";
    private const string DefaultAdminPassword = "admin123";
    private const string DefaultAdminDisplayName = "Администратор";

    public static async Task InitializeAsync(IServiceProvider services, CancellationToken ct = default)
    {
        await using var db = services.GetRequiredService<AppDbContext>();
        await db.Database.MigrateAsync(ct);

        var users = services.GetRequiredService<IUserRepository>();
        if (await users.AnyAsync(ct))
            return;

        var auth = services.GetRequiredService<IAuthService>();
        var result = await auth.RegisterAsync(
            DefaultAdminUsername, DefaultAdminPassword, DefaultAdminDisplayName,
            role: UserRole.Admin, ct: ct);

        if (result.Succeeded)
            Debug.WriteLine($"Seeded default admin '{DefaultAdminUsername}' — change this password.");
        else
            Debug.WriteLine($"Failed to seed default admin: {result.Error}");
    }
}
