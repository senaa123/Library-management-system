namespace LibraryM.Application.Fines.Models;

public sealed record FineItemDto(
    int LoanId,
    int BookId,
    string BookTitle,
    DateTime DueDate,
    DateTime? ReturnedAt,
    decimal AccruedAmount,
    decimal PaidAmount,
    decimal OutstandingAmount);

public sealed record FineSummaryDto(
    decimal TotalAccrued,
    decimal TotalPaid,
    decimal TotalOutstanding,
    IReadOnlyList<FineItemDto> Items);
