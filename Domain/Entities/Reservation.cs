using LibraryM.Domain.Enums;

namespace LibraryM.Domain.Entities;

public class Reservation
{
    public int Id { get; set; }

    public int BookId { get; set; }

    public Book? Book { get; set; }

    public int MemberId { get; set; }

    public User? Member { get; set; }

    public DateTime ReservedAt { get; set; } = DateTime.UtcNow;

    public DateTime? NotifiedAt { get; set; }

    public DateTime? CancelledAt { get; set; }

    public DateTime? FulfilledAt { get; set; }

    public ReservationStatus Status { get; set; } = ReservationStatus.Active;
}
