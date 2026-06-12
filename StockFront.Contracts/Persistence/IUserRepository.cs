namespace StockFront.Contracts.Persistence;

/// <summary>
/// Data access for accounts. The only way the auth layer reaches the <c>User</c> table — modules
/// depend on this interface from <c>Contracts</c>, never on the <c>DbContext</c> directly.
/// All access is asynchronous so the WPF UI thread is never blocked.
/// </summary>
public interface IUserRepository
{
    /// <summary>Find an account by login. Returns <c>null</c> if there is none.</summary>
    Task<UserAccount?> FindByUsernameAsync(string username, CancellationToken ct = default);

    /// <summary>True if a login is already taken (case-insensitive at the database collation).</summary>
    Task<bool> UsernameExistsAsync(string username, CancellationToken ct = default);

    /// <summary>True if a non-null e-mail is already taken.</summary>
    Task<bool> EmailExistsAsync(string email, CancellationToken ct = default);

    /// <summary>Insert a new account and return its generated id.</summary>
    Task<int> CreateAsync(NewUser user, CancellationToken ct = default);

    /// <summary>Stamp the last successful sign-in time for an account.</summary>
    Task UpdateLastLoginAsync(int userId, DateTime whenUtc, CancellationToken ct = default);

    /// <summary>Whether any account exists — used to decide if the default admin must be seeded.</summary>
    Task<bool> AnyAsync(CancellationToken ct = default);
}