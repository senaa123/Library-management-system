using LibraryM.Domain.Enums;

namespace LibraryM.Application.Transactions.Models;

public sealed class TransactionQueryRequest
{
    public int? UserId { get; set; }

    public int? BookId { get; set; }

    public TransactionType? Type { get; set; }
}
