using LibraryM.Application.Common;
using LibraryM.Application.Fines.Models;

namespace LibraryM.Application.Abstractions.Payments;

public interface IFineCheckoutGateway
{
    Task<OperationResult<CreateFineCheckoutSessionResult>> CreateSessionAsync(
        int memberId,
        string memberName,
        decimal amount,
        CancellationToken cancellationToken = default);

    Task<OperationResult<VerifiedFineCheckoutResult>> VerifyCompletedSessionAsync(
        string sessionId,
        CancellationToken cancellationToken = default);
}
