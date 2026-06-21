using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using StockFront.Contracts.Dashboard;
using StockFront.Contracts.Persistence;
using StockFront.Data.Repositories;

namespace StockFront.Data;

/// <summary>
/// Registers the data layer: the <see cref="AppDbContext"/> (MySQL via Pomelo) and the
/// repositories that implement the <c>Contracts</c> interfaces. Called once from the host.
/// </summary>
public static class DependencyInjection
{
    public static IServiceCollection AddDataLayer(this IServiceCollection services, string connectionString)
    {
        // Transient lifetime: a WPF host resolves from the root provider with no ambient scope,
        // so a per-operation context (rather than the default scoped one) avoids scope errors.
        services.AddDbContext<AppDbContext>(
            options => options
                .UseMySql(connectionString, ServerVersion.AutoDetect(connectionString))
                // DB convention: snake_case identifiers (tables, columns, indexes, keys, FKs);
                // C# stays PascalCase. The mapping is applied globally here, see stockfront-database skill.
                .UseSnakeCaseNamingConvention(),
            ServiceLifetime.Transient);

        services.AddTransient<IUserRepository, UserRepository>();
        services.AddTransient<IDashboardRepository, DashboardRepository>();
        services.AddTransient<IWarehouseRepository, WarehouseRepository>();

        return services;
    }
}
