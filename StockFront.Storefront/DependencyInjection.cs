using Microsoft.Extensions.DependencyInjection;
using StockFront.Contracts.Storefront;
using StockFront.Storefront.Services;

namespace StockFront.Storefront;

/// <summary>
/// Registers the storefront module's services. Called once from the host's DI setup. The
/// <see cref="IWarehouseService"/> it builds on is provided by the warehouse module.
/// </summary>
public static class DependencyInjection
{
    public static IServiceCollection AddStorefrontModule(this IServiceCollection services)
    {
        services.AddTransient<IStorefrontService, StorefrontService>();
        return services;
    }
}