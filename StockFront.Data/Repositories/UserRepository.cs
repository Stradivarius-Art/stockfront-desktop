using Microsoft.EntityFrameworkCore;
using StockFront.Contracts.Persistence;
using StockFront.Data.Entities;

namespace StockFront.Data.Repositories;

/// <summary>
/// EF Core implementation of <see cref="IUserRepository"/>. Maps the <see cref="User"/> entity to
/// the <c>Contracts</c> DTOs so that nothing above the data layer sees the entity itself.
/// </summary>
public sealed class UserRepository : IUserRepository
{
    private readonly AppDbContext _db;

    public UserRepository(AppDbContext db) => _db = db;

    public async Task<UserAccount?> FindByUsernameAsync(string username, CancellationToken ct = default)
    {
        var u = await _db.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Username == username, ct);

        return u is null
            ? null
            : new UserAccount(u.Id, u.Username, u.DisplayName, u.PasswordHash, u.Role, u.IsActive);
    }

    public Task<bool> UsernameExistsAsync(string username, CancellationToken ct = default) =>
        _db.Users.AsNoTracking().AnyAsync(x => x.Username == username, ct);

    public Task<bool> EmailExistsAsync(string email, CancellationToken ct = default) =>
        _db.Users.AsNoTracking().AnyAsync(x => x.Email == email, ct);

    public async Task<int> CreateAsync(NewUser user, CancellationToken ct = default)
    {
        var entity = new User
        {
            Username = user.Username,
            Email = user.Email,
            DisplayName = user.DisplayName,
            PasswordHash = user.PasswordHash,
            Role = user.Role,
            IsActive = true
        };

        _db.Users.Add(entity);
        await _db.SaveChangesAsync(ct);
        return entity.Id;
    }

    public async Task UpdateLastLoginAsync(int userId, DateTime whenUtc, CancellationToken ct = default)
    {
        // Targeted UPDATE — no need to load the row first.
        await _db.Users
            .Where(x => x.Id == userId)
            .ExecuteUpdateAsync(s => s.SetProperty(x => x.LastLoginAt, whenUtc), ct);
    }

    public Task<bool> AnyAsync(CancellationToken ct = default) =>
        _db.Users.AsNoTracking().AnyAsync(ct);
}
