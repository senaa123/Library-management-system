namespace LibraryM.Domain.Entities;

using LibraryM.Domain.Enums;

public class User
{
    public int Id { get; set; }

    public string Username { get; set; } = string.Empty;

    public string PasswordHash { get; set; } = string.Empty;

    public string FullName { get; set; } = string.Empty;

    public string Email { get; set; } = string.Empty;

    public string PhoneNumber { get; set; } = string.Empty;

    public string QrCodeValue { get; set; } = string.Empty;

    public UserRole Role { get; set; } = UserRole.Member;

    public bool IsActive { get; set; } = true;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime? UpdatedAt { get; set; }

    public ICollection<Loan> BorrowedLoans { get; set; } = new List<Loan>();

    public ICollection<Loan> IssuedLoans { get; set; } = new List<Loan>();

    public ICollection<Reservation> Reservations { get; set; } = new List<Reservation>();

    public ICollection<FinePayment> FinePayments { get; set; } = new List<FinePayment>();

    public ICollection<FinePayment> CollectedFinePayments { get; set; } = new List<FinePayment>();
}
