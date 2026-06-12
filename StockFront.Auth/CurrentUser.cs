using StockFront.Contracts.Auth;

namespace StockFront.Auth;

/// <summary>
/// The in-memory session. A single instance is registered as a singleton and exposed both as the
/// read-only <see cref="ICurrentUser"/> (for ViewModels) and the write-only
/// <see cref="ICurrentUserSession"/> (for the auth layer).
/// </summary>
public sealed class CurrentUser : ICurrentUser, ICurrentUserSession
{
    public UserIdentity? User { get; private set; }

    public bool IsAuthenticated => User is not null;

    public bool IsInRole(UserRole role) => User?.Role == role;

    public void SignIn(UserIdentity user) =>
        User = user ?? throw new ArgumentNullException(nameof(user));

    public void SignOut() => User = null;
}