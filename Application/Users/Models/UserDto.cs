namespace LibraryM.Application.Users.Models;

public sealed record UserDto(
    int Id,
    string Username,
    string FullName,
    string Email,
    string PhoneNumber,
    string Role,
    bool IsActive,
    DateTime CreatedAt,
    DateTime? UpdatedAt);
