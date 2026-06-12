namespace StockFront.Data.Entities;

/// <summary>
/// An account that can sign in to the application. The login is <see cref="Username"/>;
/// the password is never stored in clear text — only its hash. The role decides which
/// part of the system the user lands in (see <see cref="UserRole"/>).
/// </summary>
public sealed class User
{
    public int Id { get; set; }

    /// <summary>Login name. Unique across all accounts (enforced by the database).</summary>
    public string Username { get; set; } = null!;

    /// <summary>
    /// Password verifier. Holds the algorithm, salt and hash together (e.g. a PBKDF2/BCrypt
    /// string) — never the plain password. Hashing itself is done in the host, not here.
    /// </summary>
    public string PasswordHash { get; set; } = null!;

    /// <summary>Human-readable name shown in the profile and avatar (e.g. initials "КЛ").</summary>
    public string DisplayName { get; set; } = null!;

    public UserRole Role { get; set; } = UserRole.Customer;

    /// <summary>Disabled accounts stay in the table but cannot sign in.</summary>
    public bool IsActive { get; set; } = true;

    public DateTime CreatedAt { get; set; }
}
