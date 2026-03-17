using LibraryM.Application.Abstractions.Persistence;
using LibraryM.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace LibraryM.Infrastructure.Persistence.Repositories;

public sealed class FinePaymentRepository : IFinePaymentRepository
{
    private readonly LibraryContext _dbContext;

    public FinePaymentRepository(LibraryContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<IReadOnlyList<FinePayment>> GetPaymentsAsync(int? memberId, CancellationToken cancellationToken = default)
    {
        var query = BaseQuery();

        if (memberId.HasValue)
        {
            query = query.Where(payment => payment.MemberId == memberId.Value);
        }

        return await query
            .OrderByDescending(payment => payment.PaidAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<FinePayment>> GetPaymentsByLoanIdsAsync(IEnumerable<int> loanIds, CancellationToken cancellationToken = default)
    {
        var loanIdList = loanIds.Distinct().ToList();
        if (loanIdList.Count == 0)
        {
            return Array.Empty<FinePayment>();
        }

        return await BaseQuery()
            .Where(payment => payment.LoanId.HasValue && loanIdList.Contains(payment.LoanId.Value))
            .OrderByDescending(payment => payment.PaidAt)
            .ToListAsync(cancellationToken);
    }

    public Task<bool> ExistsByExternalReferenceAsync(string externalReference, CancellationToken cancellationToken = default)
    {
        var normalizedReference = Normalize(externalReference);
        return _dbContext.FinePayments.AnyAsync(payment => payment.ExternalReference.ToLower() == normalizedReference, cancellationToken);
    }

    public Task<FinePayment?> GetByExternalReferenceAsync(string externalReference, CancellationToken cancellationToken = default)
    {
        var normalizedReference = Normalize(externalReference);
        return BaseQuery().FirstOrDefaultAsync(payment => payment.ExternalReference.ToLower() == normalizedReference, cancellationToken);
    }

    public async Task AddAsync(FinePayment payment, CancellationToken cancellationToken = default) =>
        await _dbContext.FinePayments.AddAsync(payment, cancellationToken);

    private IQueryable<FinePayment> BaseQuery() =>
        _dbContext.FinePayments
            .Include(payment => payment.Loan)
                .ThenInclude(loan => loan!.Book)
            .Include(payment => payment.Member)
            .Include(payment => payment.ReceivedBy);

    private static string Normalize(string value) => value.Trim().ToLower();
}
