using LibraryM.Domain.Entities;

namespace LibraryM.Application.Abstractions.Persistence;

public interface IFinePaymentRepository
{
    Task<IReadOnlyList<FinePayment>> GetPaymentsAsync(int? memberId, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<FinePayment>> GetPaymentsByLoanIdsAsync(IEnumerable<int> loanIds, CancellationToken cancellationToken = default);

    Task<bool> ExistsByExternalReferenceAsync(string externalReference, CancellationToken cancellationToken = default);

    Task<FinePayment?> GetByExternalReferenceAsync(string externalReference, CancellationToken cancellationToken = default);

    Task AddAsync(FinePayment payment, CancellationToken cancellationToken = default);
}
