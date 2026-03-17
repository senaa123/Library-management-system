namespace LibraryM.Application.Users.Models;

public sealed record UserDto(
    int Id,
    string Username,
    string FullName,
    string Email,
    string PhoneNumber,
    string NicNumber,
    string QrCodeValue,
    string Role,
    bool IsActive,
    decimal TotalOutstandingFine,
    int MaxCirculationItems,
    bool IsCirculationBlocked,
    bool HasTemporaryRestriction,
    DateTime? RestrictedUntilUtc,
    string RestrictionReason,
    string RestrictionWarning,
    DateTime CreatedAt,
    DateTime? UpdatedAt);
