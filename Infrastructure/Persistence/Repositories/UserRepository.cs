using LibraryM.Application.Abstractions.Persistence;
using LibraryM.Domain.Entities;
using LibraryM.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace LibraryM.Infrastructure.Persistence.Repositories;

public sealed class UserRepository : IUserRepository
{
    private readonly LibraryContext _dbContext;

    public UserRepository(LibraryContext dbContext)
    {
        _dbContext = dbContext;
    }

    public Task<bool> ExistsByUsernameAsync(string username, CancellationToken cancellationToken = default)
    {
        var normalizedUsername = Normalize(username);
        return _dbContext.Users.AnyAsync(user => user.Username.ToLower() == normalizedUsername, cancellationToken);
    }

    public Task<User?> GetByUsernameAsync(string username, CancellationToken cancellationToken = default)
    {
        var normalizedUsername = Normalize(username);
        return _dbContext.Users.FirstOrDefaultAsync(user => user.Username.ToLower() == normalizedUsername, cancellationToken);
    }

    public Task<User?> GetByIdAsync(int userId, CancellationToken cancellationToken = default) =>
        _dbContext.Users.FirstOrDefaultAsync(user => user.Id == userId, cancellationToken);

    public async Task<IReadOnlyList<User>> GetAllAsync(UserRole? role, bool? isActive, CancellationToken cancellationToken = default)
    {
        var query = _dbContext.Users.AsNoTracking().AsQueryable();

        if (role.HasValue)
        {
            query = query.Where(user => user.Role == role.Value);
        }

        if (isActive.HasValue)
        {
            query = query.Where(user => user.IsActive == isActive.Value);
        }

        return await query.OrderBy(user => user.FullName).ThenBy(user => user.Username).ToListAsync(cancellationToken);
    }

    public async Task AddAsync(User user, CancellationToken cancellationToken = default) =>
        await _dbContext.Users.AddAsync(user, cancellationToken);

    private static string Normalize(string username) => username.Trim().ToLower();
}
