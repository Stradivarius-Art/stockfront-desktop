using Microsoft.Extensions.DependencyInjection;
using StockFront.Contracts.Warehouse;
using StockFront.Warehouse.Services;

namespace StockFront.Warehouse;

/// <summary>
/// Registers the warehouse module's services. Called once from the host's DI setup. The
/// <see cref="IWarehouseRepository"/> it depends on is provided by the data layer.
/// </summary>
public static class DependencyInjection
{
    public static IServiceCollection AddWarehouseModule(this IServiceCollection services)
    {
        // Transient, like the other modules: a WPF host resolves from the root provider with no
        // ambient scope, and the DbContext behind the repository is transient too.
        services.AddTransient<IWarehouseService, WarehouseService>();

        return services;
    }
}