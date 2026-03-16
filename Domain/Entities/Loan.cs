using LibraryM.Domain.Enums;

namespace LibraryM.Domain.Entities;

public class Loan
{
    public int Id { get; set; }

    public int BookId { get; set; }

    public Book? Book { get; set; }

    public int BorrowerId { get; set; }

    public User? Borrower { get; set; }

    public int IssuedById { get; set; }

    public User? IssuedBy { get; set; }

    public DateTime IssuedAt { get; set; } = DateTime.UtcNow;

    public DateTime DueDate { get; set; }

    public DateTime? ReturnedAt { get; set; }

    public int RenewCount { get; set; }

    public LoanStatus Status { get; set; } = LoanStatus.Active;

    public ICollection<FinePayment> FinePayments { get; set; } = new List<FinePayment>();
}
