namespace LibraryM.Application.Fines.Models;

public sealed record FinePaymentRecordDto(
    int Id,
    int? LoanId,
    int MemberId,
    string MemberName,
    decimal Amount,
    DateTime PaidAt,
    string Notes,
    int? ReceivedById,
    string ReceivedByName,
    string PaymentMethod,
    string ExternalReference);
