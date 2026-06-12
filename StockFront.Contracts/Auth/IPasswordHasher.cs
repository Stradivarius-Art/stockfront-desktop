namespace StockFront.Contracts.Auth;

/// <summary>
/// One-way password hashing, backed by a vetted algorithm (BCrypt/PBKDF2/Argon2). The salt is
/// embedded in the produced hash, so callers store a single string. Never used to "decrypt" a
/// password — there is no reverse.
/// </summary>
public interface IPasswordHasher
{
    /// <summary>Hash a plaintext password for storage. Two calls with the same input differ (salting).</summary>
    string Hash(string password);

    /// <summary>Verify a plaintext password against a stored hash, in constant time.</summary>
    bool Verify(string password, string hash);
}