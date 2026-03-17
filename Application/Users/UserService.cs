using LibraryM.Application.Abstractions.Authentication;
using LibraryM.Application.Abstractions.Persistence;
using LibraryM.Application.Common;
using LibraryM.Application.Fines;
using LibraryM.Application.Users.Models;
using LibraryM.Domain.Entities;
using LibraryM.Domain.Enums;

namespace LibraryM.Application.Users;

public sealed class UserService : IUserService
{
    private readonly IUserRepository _userRepository;
    private readonly IPasswordHasher _passwordHasher;
    private readonly ITransactionRepository _transactionRepository;
    private readonly IFineService _fineService;
    private readonly ILibraryUnitOfWork _unitOfWork;

    public UserService(
        IUserRepository userRepository,
        IPasswordHasher passwordHasher,
        ITransactionRepository transactionRepository,
        IFineService fineService,
        ILibraryUnitOfWork unitOfWork)
    {
        _userRepository = userRepository;
        _passwordHasher = passwordHasher;
        _transactionRepository = transactionRepository;
        _fineService = fineService;
        _unitOfWork = unitOfWork;
    }

    public async Task<OperationResult<UserDto>> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        var user = await _userRepository.GetByIdAsync(id, cancellationToken);
        if (user is null)
        {
            return OperationResult<UserDto>.Failure("User not found", FailureType.NotFound);
        }

        return OperationResult<UserDto>.Success(await MapAsync(user, cancellationToken));
    }

    public async Task<IReadOnlyList<UserDto>> GetUsersAsync(UserRole? role, bool? isActive, CancellationToken cancellationToken = default)
    {
        var users = await _userRepository.GetAllAsync(role, isActive, cancellationToken);
        var userDtos = new List<UserDto>(users.Count);

        foreach (var user in users)
        {
            userDtos.Add(await MapAsync(user, cancellationToken));
        }

        return userDtos;
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
            NicNumber = string.Empty,
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

        return OperationResult<UserDto>.Success(await MapAsync(user, cancellationToken));
    }

    public Task<OperationResult<UserDto>> UpdateProfileAsync(int userId, UpdateUserRequest request, CancellationToken cancellationToken = default) =>
        UpdateInternalAsync(userId, request, userId, allowAdministrativeChanges: false, cancellationToken);

    public Task<OperationResult<UserDto>> UpdateUserAsync(int userId, UpdateUserRequest request, int updatedByUserId, CancellationToken cancellationToken = default) =>
        UpdateInternalAsync(userId, request, updatedByUserId, allowAdministrativeChanges: true, cancellationToken);

    public async Task<OperationResult<UserDto>> SetRestrictionAsync(
        int userId,
        UpdateMemberRestrictionRequest request,
        int updatedByUserId,
        CancellationToken cancellationToken = default)
    {
        var user = await _userRepository.GetByIdAsync(userId, cancellationToken);
        if (user is null || user.Role != UserRole.Member)
        {
            return OperationResult<UserDto>.Failure("Member account not found", FailureType.NotFound);
        }

        if (request.Days < 1 || string.IsNullOrWhiteSpace(request.Reason))
        {
            return OperationResult<UserDto>.Failure("Reason and restriction days are required", FailureType.Validation);
        }

        user.RestrictedUntilUtc = DateTime.UtcNow.AddDays(request.Days);
        user.RestrictionReason = request.Reason.Trim();
        user.UpdatedAt = DateTime.UtcNow;

        await _transactionRepository.AddAsync(
            new TransactionRecord
            {
                Type = TransactionType.MemberRestrictionUpdated,
                UserId = user.Id,
                PerformedById = updatedByUserId,
                Details = $"Member '{user.Username}' was restricted until {user.RestrictedUntilUtc:yyyy-MM-dd}. Reason: {user.RestrictionReason}"
            },
            cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return OperationResult<UserDto>.Success(await MapAsync(user, cancellationToken));
    }

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

        var nicNumber = CleanOptional(request.NicNumber, user.NicNumber);
        if (!string.IsNullOrWhiteSpace(nicNumber) && await _userRepository.ExistsByNicAsync(nicNumber, user.Id, cancellationToken))
        {
            return OperationResult<UserDto>.Failure("Another account already uses this NIC number", FailureType.Conflict);
        }

        user.FullName = CleanOptional(request.FullName, user.FullName);
        user.Email = CleanOptional(request.Email, user.Email);
        user.PhoneNumber = CleanOptional(request.PhoneNumber, user.PhoneNumber);
        user.NicNumber = nicNumber;

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

        return OperationResult<UserDto>.Success(await MapAsync(user, cancellationToken));
    }

    private async Task<UserDto> MapAsync(User user, CancellationToken cancellationToken)
    {
        var fineStatus = user.Role == UserRole.Member
            ? await _fineService.GetMemberStatusAsync(user.Id, cancellationToken)
            : new Application.Fines.Models.MemberFineStatusDto(0m, 0, false, false, false, null, string.Empty, string.Empty);

        return new UserDto(
            user.Id,
            user.Username,
            user.FullName,
            user.Email,
            user.PhoneNumber,
            user.NicNumber,
            user.QrCodeValue,
            user.Role.ToString(),
            user.IsActive,
            fineStatus.TotalOutstandingFine,
            fineStatus.MaxCirculationItems,
            fineStatus.IsCirculationBlocked,
            fineStatus.HasTemporaryRestriction,
            fineStatus.RestrictedUntilUtc,
            fineStatus.RestrictionReason,
            fineStatus.WarningMessage,
            user.CreatedAt,
            user.UpdatedAt);
    }

    private static string CleanOptional(string? value, string fallback = "")
    {
        var trimmed = value?.Trim();
        return string.IsNullOrWhiteSpace(trimmed) ? fallback : trimmed;
    }
}
