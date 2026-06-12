namespace StockFront.Contracts.Auth;

/// <summary>
/// Sign-in, sign-out and registration for the desktop app. Verifies credentials against the
/// stored password hash and, on success, populates the singleton session
/// (<see cref="ICurrentUserSession"/>). There are no tokens — the app itself is the trusted client.
/// </summary>
public interface IAuthService
{
    /// <summary>
    /// Verify <paramref name="username"/> + <paramref name="password"/>. On success the session is
    /// started and the identity returned; on failure a generic error is returned (the caller must
    /// not be told which part was wrong).
    /// </summary>
    Task<AuthResult> LoginAsync(string username, string password, CancellationToken ct = default);

    /// <summary>
    /// Create a new account (defaults to the <see cref="UserRole.Customer"/> role). Fails if the
    /// username or e-mail is already taken. Does not sign the new user in.
    /// </summary>
    Task<AuthResult> RegisterAsync(
        string username,
        string password,
        string displayName,
        string? email = null,
        UserRole role = UserRole.Customer,
        CancellationToken ct = default);

    /// <summary>End the current session.</summary>
    void Logout();
}