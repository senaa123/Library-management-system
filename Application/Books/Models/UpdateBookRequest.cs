namespace LibraryM.Application.Books.Models;

public sealed class UpdateBookRequest
{
    public string? Id { get; set; }

    public string Title { get; set; } = string.Empty;

    public string Author { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;

    public string Category { get; set; } = string.Empty;
}
