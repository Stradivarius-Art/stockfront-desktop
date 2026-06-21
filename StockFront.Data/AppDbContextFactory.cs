using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace StockFront.Data;

/// <summary>
/// Design-time factory used only by the EF Core tools (<c>dotnet ef migrations add</c> /
/// <c>database update</c>). The connection string is read from the <c>.env</c> file (or a real
/// environment variable); it is never hard-coded or committed. A fixed server version is used
/// here so the tools do not need a live database to scaffold migrations.
/// </summary>
public sealed class AppDbContextFactory : IDesignTimeDbContextFactory<AppDbContext>
{
    /// <summary>Name of the environment variable / .env key holding the MySQL connection string.</summary>
    public const string ConnectionEnvVar = "STOCKFRONT_DB_CONNECTION";

    public AppDbContext CreateDbContext(string[] args)
    {
        DotEnv.Load();

        var connectionString = Environment.GetEnvironmentVariable(ConnectionEnvVar)
            ?? throw new InvalidOperationException(
                $"Connection string not found. Set the '{ConnectionEnvVar}' environment variable, " +
                "or create a .env file in the repo root (copy it from .env.example).");

        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseMySql(connectionString, new MySqlServerVersion(new Version(8, 0, 36)))
            // Must match the runtime registration (see DependencyInjection) so design-time
            // scaffolding produces snake_case identifiers too.
            .UseSnakeCaseNamingConvention()
            .Options;

        return new AppDbContext(options);
    }
}