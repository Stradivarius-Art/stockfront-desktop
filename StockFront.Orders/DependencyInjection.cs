using Microsoft.Extensions.DependencyInjection;
using StockFront.Contracts.Orders;
using StockFront.Orders.Services;

namespace StockFront.Orders;

/// <summary>
/// Registers the orders module's services. Called once from the host's DI setup. The
/// <see cref="IOrderRepository"/> it depends on is provided by the data layer.
/// </summary>
public static class DependencyInjection
{
    public static IServiceCollection AddOrdersModule(this IServiceCollection services)
    {
        services.AddTransient<IOrderService, OrderService>();
        return services;
    }
}