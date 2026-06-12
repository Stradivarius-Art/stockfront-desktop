using StockFront.Contracts.Auth;

namespace StockFront.Contracts.Persistence;

/// <summary>
/// A user row as the repository hands it to the auth layer. Carries the <see cref="PasswordHash"/>
/// because <see cref="Auth.IAuthService"/> needs it to verify a login — this DTO must never travel
/// further up than the auth layer (never into a ViewModel).
/// </summary>
public sealed record UserAccount(
    int Id,
    string Username,
    string DisplayName,
    string PasswordHash,
    UserRole Role,
    bool IsActive);