using LibraryM.Application.Abstractions.Persistence;
using LibraryM.Domain.Entities;
using LibraryM.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace LibraryM.Infrastructure.Persistence.Repositories;

public sealed class TransactionRepository : ITransactionRepository
{
    private readonly LibraryContext _dbContext;

    public TransactionRepository(LibraryContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<IReadOnlyList<TransactionRecord>> GetTransactionsAsync(int? userId, int? bookId, TransactionType? type, CancellationToken cancellationToken = default)
    {
        var query = _dbContext.TransactionRecords
            .AsNoTracking()
            .Include(transaction => transaction.Book)
            .Include(transaction => transaction.User)
            .Include(transaction => transaction.PerformedBy)
            .AsQueryable();

        if (userId.HasValue)
        {
            query = query.Where(transaction => transaction.UserId == userId.Value);
        }

        if (bookId.HasValue)
        {
            query = query.Where(transaction => transaction.BookId == bookId.Value);
        }

        if (type.HasValue)
        {
            query = query.Where(transaction => transaction.Type == type.Value);
        }

        return await query
            .OrderByDescending(transaction => transaction.OccurredAt)
            .ToListAsync(cancellationToken);
    }

    public async Task AddAsync(TransactionRecord transaction, CancellationToken cancellationToken = default) =>
        await _dbContext.TransactionRecords.AddAsync(transaction, cancellationToken);
}
