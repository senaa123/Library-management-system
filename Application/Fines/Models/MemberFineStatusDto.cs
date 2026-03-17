namespace LibraryM.Application.Fines.Models;

public sealed record MemberFineStatusDto(
    decimal TotalOutstandingFine,
    int MaxCirculationItems,
    bool IsFineLimited,
    bool IsCirculationBlocked,
    bool HasTemporaryRestriction,
    DateTime? RestrictedUntilUtc,
    string RestrictionReason,
    string WarningMessage);
