using LibraryM.Application.Common;
using LibraryM.Application.Fines.Models;
using LibraryM.Domain.Enums;

namespace LibraryM.Application.Fines;

public interface IFineService
{
    Task<OperationResult<FineSummaryDto>> GetSummaryAsync(int? memberId, int requesterUserId, UserRole requesterRole, CancellationToken cancellationToken = default);

    Task<OperationResult<IReadOnlyList<FinePaymentRecordDto>>> GetPaymentsAsync(int? memberId, int requesterUserId, UserRole requesterRole, CancellationToken cancellationToken = default);

    Task<OperationResult<FinePaymentRecordDto>> RecordPaymentAsync(FinePaymentRequest request, int receivedByUserId, CancellationToken cancellationToken = default);
}
