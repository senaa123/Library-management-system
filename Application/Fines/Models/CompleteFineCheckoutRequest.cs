using System.ComponentModel.DataAnnotations;

namespace LibraryM.Application.Fines.Models;

public sealed class CompleteFineCheckoutRequest
{
    [Required]
    public string SessionId { get; set; } = string.Empty;
}
