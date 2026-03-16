using System.ComponentModel.DataAnnotations;

namespace LibraryM.Application.Auth.Models;

public sealed class RegisterRequest
{
    [Required]
    public string Username { get; set; } = string.Empty;

    [Required]
    public string Password { get; set; } = string.Empty;

    public string? FullName { get; set; }

    [EmailAddress]
    public string? Email { get; set; }

    public string? PhoneNumber { get; set; }
}
