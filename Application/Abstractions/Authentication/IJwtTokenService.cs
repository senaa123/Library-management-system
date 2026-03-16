using LibraryM.Domain.Enums;

namespace LibraryM.Application.Abstractions.Authentication;

public interface IJwtTokenService
{
    string GenerateToken(int userId, string username, UserRole role);
}
