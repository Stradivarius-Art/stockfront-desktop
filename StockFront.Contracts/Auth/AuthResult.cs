namespace StockFront.Contracts.Auth;

/// <summary>
/// Outcome of a sign-in / registration attempt. On failure <see cref="Error"/> holds a
/// generic, user-facing message — it never reveals whether the e-mail or the password
/// was the wrong part.
/// </summary>
public sealed record AuthResult(bool Succeeded, UserIdentity? User, string? Error)
{
    public static AuthResult Success(UserIdentity user) => new(true, user, null);

    public static AuthResult Fail(string error) => new(false, null, error);
}
