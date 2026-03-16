using LibraryM.Domain.Entities;
using LibraryM.Domain.Enums;

namespace LibraryM.Application.Abstractions.Persistence;

public interface ITransactionRepository
{
    Task<IReadOnlyList<TransactionRecord>> GetTransactionsAsync(int? userId, int? bookId, TransactionType? type, CancellationToken cancellationToken = default);

    Task AddAsync(TransactionRecord transaction, CancellationToken cancellationToken = default);
}
