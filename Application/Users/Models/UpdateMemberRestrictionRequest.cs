using System.ComponentModel.DataAnnotations;

namespace LibraryM.Application.Users.Models;

public sealed class UpdateMemberRestrictionRequest
{
    [Range(1, 365)]
    public int Days { get; set; }

    [Required]
    public string Reason { get; set; } = string.Empty;
}
