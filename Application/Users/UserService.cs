using LibraryM.Application.Abstractions.Authentication;
using LibraryM.Application.Abstractions.Persistence;
using LibraryM.Application.Common;
using LibraryM.Application.Users.Models;
using LibraryM.Domain.Entities;
using LibraryM.Domain.Enums;

namespace LibraryM.Application.Users;

public sealed class UserService : IUserService
{
    private readonly IUserRepository _userRepository;
    private readonly IPasswordHasher _passwordHasher;
    private readonly ITransactionRepository _transactionRepository;
    private readonly ILibraryUnitOfWork _unitOfWork;

    public UserService(
        IUserRepository userRepository,
        IPasswordHasher passwordHasher,
        ITransactionRepository transactionRepository,
        ILibraryUnitOfWork unitOfWork)
    {
        _userRepository = userRepository;
        _passwordHasher = passwordHasher;
        _transactionRepository = transactionRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<OperationResult<UserDto>> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        var user = await _userRepository.GetByIdAsync(id, cancellationToken);
        return user is null
            ? OperationResult<UserDto>.Failure("User not found", FailureType.NotFound)
            : OperationResult<UserDto>.Success(Map(user));
    }

    public async Task<IReadOnlyList<UserDto>> GetUsersAsync(UserRole? role, bool? isActive, CancellationToken cancellationToken = default)
    {
        var users = await _userRepository.GetAllAsync(role, isActive, cancellationToken);
        return users.Select(Map).ToList();
    }

    public async Task<OperationResult<UserDto>> CreateStaffAsync(CreateStaffRequest request, int createdByUserId, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.Username) || string.IsNullOrWhiteSpace(request.Password))
        {
            return OperationResult<UserDto>.Failure("Username and password are required", FailureType.Validation);
        }

        if (request.Password.Trim().Length < 6)
        {
            return OperationResult<UserDto>.Failure("Password must be at least 6 characters long", FailureType.Validation);
        }

        if (!Enum.TryParse<UserRole>(request.Role?.Trim(), true, out var role))
        {
            return OperationResult<UserDto>.Failure("Role is invalid", FailureType.Validation);
        }

        if (role == UserRole.Member)
        {
            return OperationResult<UserDto>.Failure("Use member registration for member accounts", FailureType.Validation);
        }

        var username = request.Username.Trim();
        if (await _userRepository.ExistsByUsernameAsync(username, cancellationToken))
        {
            return OperationResult<UserDto>.Failure("User already exists", FailureType.Conflict);
        }

        var user = new User
        {
            Username = username,
            PasswordHash = _passwordHasher.Hash(request.Password),
            FullName = CleanOptional(request.FullName, username),
            Email = CleanOptional(request.Email),
            PhoneNumber = CleanOptional(request.PhoneNumber),
            Role = role,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        await _userRepository.AddAsync(user, cancellationToken);
        await _transactionRepository.AddAsync(
            new TransactionRecord
            {
                Type = TransactionType.StaffCreated,
                User = user,
                PerformedById = createdByUserId,
                Details = $"Staff account '{user.Username}' was created with role {user.Role}."
            },
            cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return OperationResult<UserDto>.Success(Map(user));
    }

    public Task<OperationResult<UserDto>> UpdateProfileAsync(int userId, UpdateUserRequest request, CancellationToken cancellationToken = default) =>
        UpdateInternalAsync(userId, request, userId, allowAdministrativeChanges: false, cancellationToken);

    public Task<OperationResult<UserDto>> UpdateUserAsync(int userId, UpdateUserRequest request, int updatedByUserId, CancellationToken cancellationToken = default) =>
        UpdateInternalAsync(userId, request, updatedByUserId, allowAdministrativeChanges: true, cancellationToken);

    private async Task<OperationResult<UserDto>> UpdateInternalAsync(
        int userId,
        UpdateUserRequest request,
        int updatedByUserId,
        bool allowAdministrativeChanges,
        CancellationToken cancellationToken)
    {
        var user = await _userRepository.GetByIdAsync(userId, cancellationToken);
        if (user is null)
        {
            return OperationResult<UserDto>.Failure("User not found", FailureType.NotFound);
        }

        if (!string.IsNullOrWhiteSpace(request.Password) && request.Password.Trim().Length < 6)
        {
            return OperationResult<UserDto>.Failure("Password must be at least 6 characters long", FailureType.Validation);
        }

        user.FullName = CleanOptional(request.FullName, user.FullName);
        user.Email = CleanOptional(request.Email, user.Email);
        user.PhoneNumber = CleanOptional(request.PhoneNumber, user.PhoneNumber);

        if (!string.IsNullOrWhiteSpace(request.Password))
        {
            user.PasswordHash = _passwordHasher.Hash(request.Password.Trim());
        }

        if (allowAdministrativeChanges)
        {
            if (!string.IsNullOrWhiteSpace(request.Role))
            {
                if (!Enum.TryParse<UserRole>(request.Role.Trim(), true, out var role))
                {
                    return OperationResult<UserDto>.Failure("Role is invalid", FailureType.Validation);
                }

                user.Role = role;
            }

            if (request.IsActive.HasValue)
            {
                user.IsActive = request.IsActive.Value;
            }
        }

        user.UpdatedAt = DateTime.UtcNow;

        await _transactionRepository.AddAsync(
            new TransactionRecord
            {
                Type = TransactionType.UserUpdated,
                UserId = user.Id,
                PerformedById = updatedByUserId,
                Details = $"User '{user.Username}' details were updated."
            },
            cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return OperationResult<UserDto>.Success(Map(user));
    }

    private static UserDto Map(User user) =>
        new(
            user.Id,
            user.Username,
            user.FullName,
            user.Email,
            user.PhoneNumber,
            user.Role.ToString(),
            user.IsActive,
            user.CreatedAt,
            user.UpdatedAt);

    private static string CleanOptional(string? value, string fallback = "")
    {
        var trimmed = value?.Trim();
        return string.IsNullOrWhiteSpace(trimmed) ? fallback : trimmed;
    }
}
