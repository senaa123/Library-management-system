using System.ComponentModel.DataAnnotations;

namespace LibraryM.Application.Loans.Models;

public sealed class IssueLoanByQrRequest
{
    [Range(1, int.MaxValue)]
    public int BookId { get; set; }

    [Required]
    public string MemberQrCodeValue { get; set; } = string.Empty;

    [Range(1, 14)]
    public int BorrowDays { get; set; } = 14;
}
