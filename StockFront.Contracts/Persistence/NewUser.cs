using StockFront.Contracts.Auth;

namespace StockFront.Contracts.Persistence;

/// <summary>
/// Data needed to create a user row. The password is already hashed (the repository stores hashes,
/// never plaintext); hashing happens in the auth layer before this reaches the repository.
/// </summary>
public sealed record NewUser(
    string Email,
    string DisplayName,
    string PasswordHash,
    UserRole Role);