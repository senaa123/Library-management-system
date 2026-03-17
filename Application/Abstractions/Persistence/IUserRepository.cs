using LibraryM.Domain.Entities;
using LibraryM.Domain.Enums;

namespace LibraryM.Application.Abstractions.Persistence;

public interface IUserRepository
{
    Task<bool> ExistsByUsernameAsync(string username, CancellationToken cancellationToken = default);

    Task<bool> ExistsByQrCodeAsync(string qrCodeValue, CancellationToken cancellationToken = default);

    Task<bool> ExistsByNicAsync(string nicNumber, int? excludedUserId = null, CancellationToken cancellationToken = default);

    Task<User?> GetByUsernameAsync(string username, CancellationToken cancellationToken = default);

    Task<User?> GetByQrCodeAsync(string qrCodeValue, CancellationToken cancellationToken = default);

    Task<User?> GetByIdAsync(int userId, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<User>> GetAllAsync(UserRole? role, bool? isActive, CancellationToken cancellationToken = default);

    Task AddAsync(User user, CancellationToken cancellationToken = default);
}
