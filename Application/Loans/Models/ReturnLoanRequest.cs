using System.ComponentModel.DataAnnotations;

namespace LibraryM.Application.Loans.Models;

public sealed class ReturnLoanRequest
{
    public bool AddFine { get; set; }

    [RegularExpression("DamagedBook|LostBook|MissingPages", ErrorMessage = "Fine type is invalid.")]
    public string? FineType { get; set; }
}
