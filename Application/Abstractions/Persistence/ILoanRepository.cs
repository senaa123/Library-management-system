using LibraryM.Domain.Entities;

namespace LibraryM.Application.Abstractions.Persistence;

public interface ILoanRepository
{
    Task<Loan?> GetByIdAsync(int id, CancellationToken cancellationToken = default);

    Task<Loan?> GetActiveLoanAsync(int bookId, int memberId, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Loan>> GetLoansAsync(int? memberId, bool activeOnly, CancellationToken cancellationToken = default);

    Task AddAsync(Loan loan, CancellationToken cancellationToken = default);
}
