namespace LibraryM.Domain.Entities;

public class FinePayment
{
    public int Id { get; set; }

    public int LoanId { get; set; }

    public Loan? Loan { get; set; }

    public int MemberId { get; set; }

    public User? Member { get; set; }

    public int ReceivedById { get; set; }

    public User? ReceivedBy { get; set; }

    public decimal Amount { get; set; }

    public DateTime PaidAt { get; set; } = DateTime.UtcNow;

    public string Notes { get; set; } = string.Empty;
}
