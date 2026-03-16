using LibraryM.Application.Abstractions.Persistence;
using LibraryM.Domain.Entities;
using LibraryM.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace LibraryM.Infrastructure.Persistence.Repositories;

public sealed class LoanRepository : ILoanRepository
{
    private readonly LibraryContext _dbContext;

    public LoanRepository(LibraryContext dbContext)
    {
        _dbContext = dbContext;
    }

    public Task<Loan?> GetByIdAsync(int id, CancellationToken cancellationToken = default) =>
        BaseQuery().FirstOrDefaultAsync(loan => loan.Id == id, cancellationToken);

    public Task<Loan?> GetActiveLoanAsync(int bookId, int memberId, CancellationToken cancellationToken = default) =>
        BaseQuery().FirstOrDefaultAsync(
            loan => loan.BookId == bookId && loan.BorrowerId == memberId && loan.Status == LoanStatus.Active,
            cancellationToken);

    public async Task<IReadOnlyList<Loan>> GetLoansAsync(int? memberId, bool activeOnly, CancellationToken cancellationToken = default)
    {
        var query = BaseQuery();

        if (memberId.HasValue)
        {
            query = query.Where(loan => loan.BorrowerId == memberId.Value);
        }

        if (activeOnly)
        {
            query = query.Where(loan => loan.Status == LoanStatus.Active);
        }

        return await query
            .OrderByDescending(loan => loan.IssuedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task AddAsync(Loan loan, CancellationToken cancellationToken = default) =>
        await _dbContext.Loans.AddAsync(loan, cancellationToken);

    private IQueryable<Loan> BaseQuery() =>
        _dbContext.Loans
            .Include(loan => loan.Book)
            .Include(loan => loan.Borrower)
            .Include(loan => loan.IssuedBy)
            .Include(loan => loan.FinePayments);
}
