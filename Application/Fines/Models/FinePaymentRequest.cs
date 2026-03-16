using System.ComponentModel.DataAnnotations;

namespace LibraryM.Application.Fines.Models;

public sealed class FinePaymentRequest
{
    [Range(1, int.MaxValue)]
    public int LoanId { get; set; }

    [Range(typeof(decimal), "0.01", "9999999")]
    public decimal Amount { get; set; }

    public string? Notes { get; set; }
}
