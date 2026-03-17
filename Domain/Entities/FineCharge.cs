using LibraryM.Domain.Enums;

namespace LibraryM.Domain.Entities;

public class FineCharge
{
    public int Id { get; set; }

    public int MemberId { get; set; }

    public User? Member { get; set; }

    public int? LoanId { get; set; }

    public Loan? Loan { get; set; }

    public int? ReservationId { get; set; }

    public Reservation? Reservation { get; set; }

    public int? CreatedById { get; set; }

    public User? CreatedBy { get; set; }

    public FineChargeType ChargeType { get; set; }

    public decimal Amount { get; set; }

    public string Description { get; set; } = string.Empty;

    public string ExternalReference { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
