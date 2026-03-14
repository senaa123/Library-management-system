using LibraryM.Application.Auth.Models;
using LibraryM.Application.Common;

namespace LibraryM.Application.Auth;

public interface IAuthService
{
    Task<OperationResult> RegisterAsync(RegisterRequest request, CancellationToken cancellationToken = default);

    Task<OperationResult<AuthResponse>> LoginAsync(LoginRequest request, CancellationToken cancellationToken = default);
}
