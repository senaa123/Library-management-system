namespace LibraryM.Domain.Enums;

public enum TransactionType
{
    MemberRegistered = 1,
    StaffCreated = 2,
    UserUpdated = 3,
    BookAdded = 4,
    BookUpdated = 5,
    BookDeleted = 6,
    Issue = 7,
    Return = 8,
    Renew = 9,
    ReservationPlaced = 10,
    ReservationCancelled = 11,
    ReservationAvailable = 12,
    FinePayment = 13,
    ReservationExpired = 14,
    FineChargeAdded = 15,
    MemberRestrictionUpdated = 16
}
