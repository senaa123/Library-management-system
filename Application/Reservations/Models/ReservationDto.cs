namespace LibraryM.Application.Reservations.Models;

public sealed record ReservationDto(
    int Id,
    int BookId,
    string BookTitle,
    int MemberId,
    string MemberName,
    string MemberUsername,
    string MemberPhoneNumber,
    DateTime ReservedAt,
    DateTime? NotifiedAt,
    DateTime? CancelledAt,
    DateTime? FulfilledAt,
    DateTime? PickupDeadline,
    int DaysLeft,
    string TimeLeftLabel,
    bool CanIssue,
    string Status);
