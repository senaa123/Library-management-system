using LibraryM.Application.Abstractions.Authentication;
using LibraryM.Application.Abstractions.Persistence;
using LibraryM.Application.Auth.Models;
using LibraryM.Application.Common;
using LibraryM.Domain.Entities;

namespace LibraryM.Application.Auth;

public sealed class AuthService : IAuthService
{
    private readonly IUserRepository _userRepository;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IJwtTokenService _jwtTokenService;

    public AuthService(
        IUserRepository userRepository,
        IPasswordHasher passwordHasher,
        IJwtTokenService jwtTokenService)
    {
        _userRepository = userRepository;
        _passwordHasher = passwordHasher;
        _jwtTokenService = jwtTokenService;
    }

    public async Task<OperationResult> RegisterAsync(RegisterRequest request, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.Username) || string.IsNullOrWhiteSpace(request.Password))
        {
            return OperationResult.Failure("Username and Password are required", FailureType.Validation);
        }

        var username = request.Username.Trim();

        if (await _userRepository.ExistsByUsernameAsync(username, cancellationToken))
        {
            return OperationResult.Failure("User already exists", FailureType.Validation);
        }

        var user = new User
        {
            Username = username,
            PasswordHash = _passwordHasher.Hash(request.Password),
            Role = "User"
        };

        await _userRepository.AddAsync(user, cancellationToken);

        return OperationResult.Success("User Registered Successfully");
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

        if (!_passwordHasher.Verify(request.Password, user.PasswordHash))
        {
            return OperationResult<AuthResponse>.Failure("Invalid Password", FailureType.Unauthorized);
        }

        var token = _jwtTokenService.GenerateToken(user.Id, user.Username);
        var response = new AuthResponse(token, user.Username, user.Role);

        return OperationResult<AuthResponse>.Success(response);
    }
}
