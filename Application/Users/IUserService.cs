using LibraryM.Application.Common;
using LibraryM.Application.Users.Models;
using LibraryM.Domain.Enums;

namespace LibraryM.Application.Users;

public interface IUserService
{
    Task<OperationResult<UserDto>> GetByIdAsync(int id, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<UserDto>> GetUsersAsync(UserRole? role, bool? isActive, CancellationToken cancellationToken = default);

    Task<OperationResult<UserDto>> CreateStaffAsync(CreateStaffRequest request, int createdByUserId, CancellationToken cancellationToken = default);

    Task<OperationResult<UserDto>> UpdateProfileAsync(int userId, UpdateUserRequest request, CancellationToken cancellationToken = default);

    Task<OperationResult<UserDto>> UpdateUserAsync(int userId, UpdateUserRequest request, int updatedByUserId, CancellationToken cancellationToken = default);

    Task<OperationResult<UserDto>> SetRestrictionAsync(int userId, UpdateMemberRestrictionRequest request, int updatedByUserId, CancellationToken cancellationToken = default);
}
