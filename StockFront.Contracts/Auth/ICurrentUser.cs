namespace StockFront.Contracts.Auth;

/// <summary>
/// The signed-in user for the current session — a singleton in-memory object that <i>is</i>
/// the session (a 2-tier desktop app has no token). ViewModels and modules read access rights
/// from here; they never touch the <c>User</c> table or a password hash directly.
/// </summary>
public interface ICurrentUser
{
    /// <summary>True once a user has signed in.</summary>
    bool IsAuthenticated { get; }

    /// <summary>The signed-in identity, or <c>null</c> when no one is signed in.</summary>
    UserIdentity? User { get; }

    /// <summary>Convenience check: is the current user in <paramref name="role"/>?</summary>
    bool IsInRole(UserRole role);
}
