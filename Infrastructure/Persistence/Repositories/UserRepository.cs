using LibraryM.Application.Abstractions.Persistence;
using LibraryM.Domain.Entities;
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

    public async Task<User> AddAsync(User user, CancellationToken cancellationToken = default)
    {
        _dbContext.Users.Add(user);
        await _dbContext.SaveChangesAsync(cancellationToken);
        return user;
    }

    private static string Normalize(string username) => username.Trim().ToLower();
}
