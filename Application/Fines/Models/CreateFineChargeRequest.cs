using LibraryM.Domain.Enums;

namespace LibraryM.Application.Fines.Models;

public sealed record CreateFineChargeRequest(
    int MemberId,
    FineChargeType ChargeType,
    decimal Amount,
    string Description,
    int? CreatedByUserId = null,
    int? LoanId = null,
    int? ReservationId = null,
    string? ExternalReference = null);
