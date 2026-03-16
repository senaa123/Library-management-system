using LibraryM.Application.Abstractions.Persistence;
using LibraryM.Application.Common;
using LibraryM.Application.Transactions.Models;
using LibraryM.Domain.Enums;

namespace LibraryM.Application.Transactions;

public sealed class TransactionService : ITransactionService
{
    private readonly ITransactionRepository _transactionRepository;

    public TransactionService(ITransactionRepository transactionRepository)
    {
        _transactionRepository = transactionRepository;
    }

    public async Task<OperationResult<IReadOnlyList<TransactionDto>>> GetTransactionsAsync(
        TransactionQueryRequest request,
        int requesterUserId,
        UserRole requesterRole,
        CancellationToken cancellationToken = default)
    {
        if (requesterRole == UserRole.Member && request.UserId.HasValue && request.UserId.Value != requesterUserId)
        {
            return OperationResult<IReadOnlyList<TransactionDto>>.Failure("You can only view your own transaction history", FailureType.Forbidden);
        }

        var effectiveUserId = requesterRole == UserRole.Member ? requesterUserId : request.UserId;
        var transactions = await _transactionRepository.GetTransactionsAsync(effectiveUserId, request.BookId, request.Type, cancellationToken);
        var transactionDtos = transactions
            .Select(transaction => new TransactionDto(
                transaction.Id,
                transaction.Type.ToString(),
                transaction.BookId,
                transaction.Book?.Title ?? string.Empty,
                transaction.UserId,
                transaction.User?.Username ?? string.Empty,
                transaction.PerformedById,
                transaction.PerformedBy?.Username ?? string.Empty,
                transaction.LoanId,
                transaction.ReservationId,
                transaction.FinePaymentId,
                transaction.Details,
                transaction.OccurredAt))
            .ToList();

        return OperationResult<IReadOnlyList<TransactionDto>>.Success(transactionDtos);
    }
}
