using LibraryM.Domain.Entities;

namespace LibraryM.Application.Abstractions.Persistence;

public interface IFineChargeRepository
{
    Task<IReadOnlyList<FineCharge>> GetChargesAsync(int memberId, CancellationToken cancellationToken = default);

    Task<bool> ExistsByExternalReferenceAsync(string externalReference, CancellationToken cancellationToken = default);

    Task AddAsync(FineCharge charge, CancellationToken cancellationToken = default);
}
