using System.ComponentModel.DataAnnotations;

namespace LibraryM.Application.Users.Models;

public sealed class UpdateUserRequest
{
    public string? FullName { get; set; }

    [EmailAddress]
    public string? Email { get; set; }

    public string? PhoneNumber { get; set; }

    public string? Password { get; set; }

    public string? Role { get; set; }

    public bool? IsActive { get; set; }
}
