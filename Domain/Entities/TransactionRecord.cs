using LibraryM.Domain.Enums;

namespace LibraryM.Domain.Entities;

public class TransactionRecord
{
    public int Id { get; set; }

    public TransactionType Type { get; set; }

    public int? BookId { get; set; }

    public Book? Book { get; set; }

    public int? UserId { get; set; }

    public User? User { get; set; }

    public int? PerformedById { get; set; }

    public User? PerformedBy { get; set; }

    public int? LoanId { get; set; }

    public Loan? Loan { get; set; }

    public int? ReservationId { get; set; }

    public Reservation? Reservation { get; set; }

    public int? FinePaymentId { get; set; }

    public FinePayment? FinePayment { get; set; }

    public string Details { get; set; } = string.Empty;

    public DateTime OccurredAt { get; set; } = DateTime.UtcNow;
}
