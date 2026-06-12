using Microsoft.Extensions.DependencyInjection;
using StockFront.Auth.Services;
using StockFront.Contracts.Auth;

namespace StockFront.Auth;

/// <summary>
/// Registers the auth module's services. Called once from the host's DI setup. The session is a
/// singleton (it <i>is</i> the running session) and the same instance answers both
/// <see cref="ICurrentUser"/> and <see cref="ICurrentUserSession"/>.
/// </summary>
public static class DependencyInjection
{
    public static IServiceCollection AddAuthModule(this IServiceCollection services)
    {
        services.AddSingleton<CurrentUser>();
        services.AddSingleton<ICurrentUser>(sp => sp.GetRequiredService<CurrentUser>());
        services.AddSingleton<ICurrentUserSession>(sp => sp.GetRequiredService<CurrentUser>());

        services.AddSingleton<IPasswordHasher, BCryptPasswordHasher>();

        // Transient (not scoped): a WPF host has no ambient request scope, so we resolve auth
        // services straight from the root provider. The DbContext behind the repository is
        // registered transient in the host for the same reason.
        services.AddTransient<IAuthService, AuthService>();

        return services;
    }
}
