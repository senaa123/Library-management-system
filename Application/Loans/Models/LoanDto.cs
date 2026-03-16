namespace LibraryM.Application.Loans.Models;

public sealed record LoanDto(
    int Id,
    int BookId,
    string BookTitle,
    string Isbn,
    int BorrowerId,
    string BorrowerName,
    string BorrowerUsername,
    string BorrowerPhoneNumber,
    int IssuedById,
    string IssuedByName,
    DateTime IssuedAt,
    DateTime DueDate,
    DateTime? ReturnedAt,
    int RenewCount,
    string Status,
    decimal OutstandingFine,
    int BorrowPeriodDays,
    int DaysLeft,
    string TimeLeftLabel);
