using StockFront.Contracts.Auth;

namespace StockFront.Auth.Services;

/// <summary>
/// <see cref="IPasswordHasher"/> backed by BCrypt (BCrypt.Net-Next). BCrypt is deliberately slow,
/// embeds a per-password salt in the hash string, and does its own constant-time comparison on
/// verify — so we never compare hashes ourselves.
/// </summary>
public sealed class BCryptPasswordHasher : IPasswordHasher
{
    // Work factor 12: a sensible 2020s default — slow enough to resist brute force, fast enough
    // for an interactive login.
    private const int WorkFactor = 12;

    public string Hash(string password) =>
        BCrypt.Net.BCrypt.HashPassword(password, WorkFactor);

    public bool Verify(string password, string hash)
    {
        // A malformed stored hash must read as "wrong password", not throw.
        try
        {
            return BCrypt.Net.BCrypt.Verify(password, hash);
        }
        catch (BCrypt.Net.SaltParseException)
        {
            return false;
        }
    }
}