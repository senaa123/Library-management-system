using System.ComponentModel.DataAnnotations;

namespace LibraryM.Application.Books.Models;

public sealed class CreateBookRequest
{
    [Required]
    public string Title { get; set; } = string.Empty;

    [Required]
    public string Author { get; set; } = string.Empty;

    [Required]
    public string Description { get; set; } = string.Empty;

    [Required]
    public string Category { get; set; } = string.Empty;

    public string? Isbn { get; set; }

    public string? BookType { get; set; }

    public int? TotalCopies { get; set; }

    public int? AvailableCopies { get; set; }

    public bool IsActive { get; set; } = true;
}
