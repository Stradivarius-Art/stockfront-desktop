namespace StockFront.Contracts.Auth;

/// <summary>
/// The write side of the session, kept separate from <see cref="ICurrentUser"/> so that
/// ViewModels can only <i>read</i> who is signed in. Only the auth layer mutates the session.
/// The same singleton instance implements both interfaces.
/// </summary>
public interface ICurrentUserSession
{
    /// <summary>Begin a session for <paramref name="user"/> (called by <see cref="IAuthService"/> on success).</summary>
    void SignIn(UserIdentity user);

    /// <summary>End the current session.</summary>
    void SignOut();
}