using System.ComponentModel.DataAnnotations;

namespace LibraryM.Application.Users.Models;

public sealed class CreateStaffRequest
{
    [Required]
    public string Username { get; set; } = string.Empty;

    [Required]
    public string Password { get; set; } = string.Empty;

    public string? FullName { get; set; }

    [EmailAddress]
    public string? Email { get; set; }

    public string? PhoneNumber { get; set; }

    [Required]
    public string Role { get; set; } = string.Empty;
}
