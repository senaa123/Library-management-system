namespace LibraryM.Application.Fines.Models;

public sealed record FineItemDto(
    int? LoanId,
    int? ReservationId,
    int? BookId,
    string BookTitle,
    string FineType,
    string Description,
    DateTime AssessedAt,
    DateTime DueDate,
    DateTime? ReturnedAt,
    decimal AccruedAmount,
    decimal PaidAmount,
    decimal OutstandingAmount);

public sealed record FineSummaryDto(
    decimal TotalAccrued,
    decimal TotalPaid,
    decimal TotalOutstanding,
    MemberFineStatusDto Status,
    IReadOnlyList<FineItemDto> Items);
