using LibraryM.Domain.Entities;

namespace LibraryM.Application.Abstractions.Persistence;

public interface IUserRepository
{
    Task<bool> ExistsByUsernameAsync(string username, CancellationToken cancellationToken = default);

    Task<User?> GetByUsernameAsync(string username, CancellationToken cancellationToken = default);

    Task<User> AddAsync(User user, CancellationToken cancellationToken = default);
}
