using StockFront.Contracts.Auth;
using StockFront.Contracts.Persistence;

namespace StockFront.Auth.Services;

/// <summary>
/// Local (2-tier) authentication: verify a password against the stored hash, then populate the
/// in-memory session. No tokens — the desktop app is the trusted client.
/// </summary>
public sealed class AuthService : IAuthService
{
    // One generic message for every failure mode, so we never reveal whether the e-mail exists
    // or the account is disabled.
    private const string InvalidCredentials = "Неверный e-mail или пароль.";

    private readonly IUserRepository _users;
    private readonly IPasswordHasher _hasher;
    private readonly ICurrentUserSession _session;

    public AuthService(IUserRepository users, IPasswordHasher hasher, ICurrentUserSession session)
    {
        _users = users;
        _hasher = hasher;
        _session = session;
    }

    public async Task<AuthResult> LoginAsync(string email, string password, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(email) || string.IsNullOrEmpty(password))
            return AuthResult.Fail(InvalidCredentials);

        var account = await _users.FindByEmailAsync(email.Trim(), ct);

        // Verify even when the account is missing/inactive would let us short-circuit, but we still
        // run a hash check against a dummy value to keep timing roughly constant and not leak
        // "user exists" through response time.
        var hashToCheck = account?.PasswordHash ?? DummyHash;
        var passwordOk = _hasher.Verify(password, hashToCheck);

        if (account is null || !account.IsActive || !passwordOk)
            return AuthResult.Fail(InvalidCredentials);

        var identity = new UserIdentity(account.Id, account.Email, account.DisplayName, account.Role);
        _session.SignIn(identity);
        await _users.UpdateLastLoginAsync(account.Id, DateTime.UtcNow, ct);

        return AuthResult.Success(identity);
    }

    public async Task<AuthResult> RegisterAsync(
        string email,
        string password,
        string displayName,
        UserRole role = UserRole.Customer,
        CancellationToken ct = default)
    {
        // Same rules the UI shows — enforced here too, so they hold when the UI is bypassed.
        var error = CredentialRules.ValidateEmail(email)
                    ?? CredentialRules.ValidatePassword(password)
                    ?? CredentialRules.ValidateDisplayName(displayName);
        if (error is not null)
            return AuthResult.Fail(error);

        email = email.Trim();

        if (await _users.EmailExistsAsync(email, ct))
            return AuthResult.Fail("E-mail уже используется.");

        var newUser = new NewUser(email, displayName.Trim(), _hasher.Hash(password), role);
        var id = await _users.CreateAsync(newUser, ct);

        return AuthResult.Success(new UserIdentity(id, email, displayName.Trim(), role));
    }

    public void Logout() => _session.SignOut();

    // A fixed valid BCrypt hash (of a random string) used only to spend time verifying when the
    // account doesn't exist, so failed logins for real and unknown e-mails take similar time.
    private const string DummyHash = "$2a$12$C6UzMDM.H6dfI/f/IKcEeO.7Z3Q3p3VvJ8m6m3Yk5o9oQpQ4q3qK";
}
