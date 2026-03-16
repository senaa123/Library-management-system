using LibraryM.Application.Common;
using LibraryM.Application.Transactions.Models;
using LibraryM.Domain.Enums;

namespace LibraryM.Application.Transactions;

public interface ITransactionService
{
    Task<OperationResult<IReadOnlyList<TransactionDto>>> GetTransactionsAsync(TransactionQueryRequest request, int requesterUserId, UserRole requesterRole, CancellationToken cancellationToken = default);
}
