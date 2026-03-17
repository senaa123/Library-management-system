using LibraryM.Application.Abstractions.Authentication;
using LibraryM.Application.Abstractions.Persistence;
using LibraryM.Application.Auth.Models;
using LibraryM.Application.Common;
using LibraryM.Domain.Entities;
using LibraryM.Domain.Enums;

namespace LibraryM.Application.Auth;

public sealed class AuthService : IAuthService
{
    private readonly IUserRepository _userRepository;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IJwtTokenService _jwtTokenService;
    private readonly ITransactionRepository _transactionRepository;
    private readonly ILibraryUnitOfWork _unitOfWork;

    public AuthService(
        IUserRepository userRepository,
        IPasswordHasher passwordHasher,
        IJwtTokenService jwtTokenService,
        ITransactionRepository transactionRepository,
        ILibraryUnitOfWork unitOfWork)
    {
        _userRepository = userRepository;
        _passwordHasher = passwordHasher;
        _jwtTokenService = jwtTokenService;
        _transactionRepository = transactionRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<OperationResult<RegisterResponse>> RegisterAsync(RegisterRequest request, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.Username) || string.IsNullOrWhiteSpace(request.Password))
        {
            return OperationResult<RegisterResponse>.Failure("Username and Password are required", FailureType.Validation);
        }

        var username = request.Username.Trim();
        if (request.Password.Trim().Length < 6)
        {
            return OperationResult<RegisterResponse>.Failure("Password must be at least 6 characters long", FailureType.Validation);
        }

        if (await _userRepository.ExistsByUsernameAsync(username, cancellationToken))
        {
            return OperationResult<RegisterResponse>.Failure("User already exists", FailureType.Conflict);
        }

        if (string.IsNullOrWhiteSpace(request.NicNumber))
        {
            return OperationResult<RegisterResponse>.Failure("NIC number is required", FailureType.Validation);
        }

        var nicNumber = request.NicNumber.Trim();
        if (await _userRepository.ExistsByNicAsync(nicNumber, cancellationToken: cancellationToken))
        {
            return OperationResult<RegisterResponse>.Failure("An account already exists for this NIC number", FailureType.Conflict);
        }

        var qrCodeValue = await GenerateQrCodeValueAsync(cancellationToken);
        var user = new User
        {
            Username = username,
            PasswordHash = _passwordHasher.Hash(request.Password),
            FullName = CleanOptional(request.FullName, username),
            Email = CleanOptional(request.Email),
            PhoneNumber = CleanOptional(request.PhoneNumber),
            NicNumber = nicNumber,
            QrCodeValue = qrCodeValue,
            Role = UserRole.Member,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        await _userRepository.AddAsync(user, cancellationToken);
        await _transactionRepository.AddAsync(
            new TransactionRecord
            {
                Type = TransactionType.MemberRegistered,
                User = user,
                PerformedBy = user,
                Details = $"Member '{user.Username}' registered."
            },
            cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return OperationResult<RegisterResponse>.Success(
            new RegisterResponse("User Registered Successfully", user.Id, user.Username, user.FullName, user.QrCodeValue));
    }

    public async Task<OperationResult<AuthResponse>> LoginAsync(LoginRequest request, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.Username) || string.IsNullOrWhiteSpace(request.Password))
        {
            return OperationResult<AuthResponse>.Failure("Username and Password are required", FailureType.Validation);
        }

        var username = request.Username.Trim();
        var user = await _userRepository.GetByUsernameAsync(username, cancellationToken);

        if (user is null)
        {
            return OperationResult<AuthResponse>.Failure("Invalid Username", FailureType.Unauthorized);
        }

        if (!user.IsActive)
        {
            return OperationResult<AuthResponse>.Failure("This account is inactive", FailureType.Forbidden);
        }

        if (!_passwordHasher.Verify(request.Password, user.PasswordHash))
        {
            return OperationResult<AuthResponse>.Failure("Invalid Password", FailureType.Unauthorized);
        }

        var token = _jwtTokenService.GenerateToken(user.Id, user.Username, user.Role);
        var response = new AuthResponse(user.Id, token, user.Username, user.FullName, user.Role.ToString(), user.QrCodeValue);

        return OperationResult<AuthResponse>.Success(response);
    }

    private async Task<string> GenerateQrCodeValueAsync(CancellationToken cancellationToken)
    {
        string qrCodeValue;
        do
        {
            qrCodeValue = $"LIBMEM-{Guid.NewGuid():N}";
        }
        while (await _userRepository.ExistsByQrCodeAsync(qrCodeValue, cancellationToken));

        return qrCodeValue;
    }

    private static string CleanOptional(string? value, string? fallback = null)
    {
        var trimmed = value?.Trim();
        if (!string.IsNullOrWhiteSpace(trimmed))
        {
            return trimmed;
        }

        return fallback?.Trim() ?? string.Empty;
    }
}
