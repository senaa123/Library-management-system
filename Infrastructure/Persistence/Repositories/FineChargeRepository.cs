using LibraryM.Application.Abstractions.Persistence;
using LibraryM.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace LibraryM.Infrastructure.Persistence.Repositories;

public sealed class FineChargeRepository : IFineChargeRepository
{
    private readonly LibraryContext _dbContext;

    public FineChargeRepository(LibraryContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<IReadOnlyList<FineCharge>> GetChargesAsync(int memberId, CancellationToken cancellationToken = default)
    {
        return await _dbContext.FineCharges
            .Include(charge => charge.Loan)
                .ThenInclude(loan => loan!.Book)
            .Include(charge => charge.Reservation)
                .ThenInclude(reservation => reservation!.Book)
            .Include(charge => charge.CreatedBy)
            .Where(charge => charge.MemberId == memberId)
            .OrderBy(charge => charge.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public Task<bool> ExistsByExternalReferenceAsync(string externalReference, CancellationToken cancellationToken = default)
    {
        var normalizedReference = Normalize(externalReference);
        return _dbContext.FineCharges.AnyAsync(charge => charge.ExternalReference.ToLower() == normalizedReference, cancellationToken);
    }

    public async Task AddAsync(FineCharge charge, CancellationToken cancellationToken = default) =>
        await _dbContext.FineCharges.AddAsync(charge, cancellationToken);

    private static string Normalize(string value) => value.Trim().ToLower();
}
