using System.ComponentModel.DataAnnotations;

namespace LibraryM.Application.Loans.Models;

public sealed class IssueLoanRequest
{
    [Range(1, int.MaxValue)]
    public int BookId { get; set; }

    [Range(1, int.MaxValue)]
    public int MemberId { get; set; }

    [Range(1, 14)]
    public int BorrowDays { get; set; } = 14;
}
