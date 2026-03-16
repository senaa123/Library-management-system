namespace LibraryM.Application.Reservations.Models;

public sealed record ReservationDto(
    int Id,
    int BookId,
    string BookTitle,
    int MemberId,
    string MemberName,
    string MemberUsername,
    DateTime ReservedAt,
    DateTime? NotifiedAt,
    DateTime? CancelledAt,
    DateTime? FulfilledAt,
    string Status);
