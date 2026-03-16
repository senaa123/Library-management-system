namespace LibraryM.Application.Transactions.Models;

public sealed record TransactionDto(
    int Id,
    string Type,
    int? BookId,
    string BookTitle,
    int? UserId,
    string Username,
    int? PerformedById,
    string PerformedByUsername,
    int? LoanId,
    int? ReservationId,
    int? FinePaymentId,
    string Details,
    DateTime OccurredAt);
