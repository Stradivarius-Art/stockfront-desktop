using System.Diagnostics;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using StockFront.Contracts.Auth;
using StockFront.Contracts.Persistence;
using StockFront.Data;
using StockFront.Data.Seeding;

namespace stockfront.Infrastructure;

/// <summary>
/// Database maintenance operations. These are deliberate, opt-in actions invoked from the terminal
/// (see <see cref="CommandLineRunner"/>) — never from the normal application startup path, so a
/// regular launch never migrates or writes seed data and can't clobber real user data.
/// </summary>
public static class DatabaseBootstrapper
{
    // Default administrator seeded only into an empty users table. MUST be changed after the first
    // sign-in; it exists so a fresh database isn't locked out of its own admin area.
    private const string DefaultAdminEmail = "admin@stockfront.local";
    private const string DefaultAdminPassword = "admin123";
    private const string DefaultAdminDisplayName = "Администратор";

    /// <summary>Bring the schema up to date by applying any pending EF Core migrations.</summary>
    public static async Task MigrateAsync(IServiceProvider services, CancellationToken ct = default)
    {
        await using var db = services.GetRequiredService<AppDbContext>();
        await db.Database.MigrateAsync(ct);
    }

    /// <summary>
    /// Create the default administrator if (and only if) the users table is empty. Idempotent: does
    /// nothing once any account exists, so it never overwrites real users.
    /// </summary>
    public static async Task SeedAdminAsync(IServiceProvider services, CancellationToken ct = default)
    {
        var users = services.GetRequiredService<IUserRepository>();
        if (await users.AnyAsync(ct))
        {
            Debug.WriteLine("Users already exist — default admin not seeded.");
            return;
        }

        var auth = services.GetRequiredService<IAuthService>();
        var result = await auth.RegisterAsync(
            DefaultAdminEmail, DefaultAdminPassword, DefaultAdminDisplayName,
            role: UserRole.Admin, ct: ct);

        if (result.Succeeded)
            Debug.WriteLine($"Seeded default admin '{DefaultAdminEmail}' — change this password.");
        else
            throw new InvalidOperationException($"Failed to seed default admin: {result.Error}");
    }

    /// <summary>Fill the database with illustrative demo data (idempotent — see TestDataSeeder).</summary>
    public static async Task SeedTestDataAsync(IServiceProvider services, CancellationToken ct = default)
    {
        await using var db = services.GetRequiredService<AppDbContext>();
        await TestDataSeeder.SeedAsync(db, ct);
    }
}