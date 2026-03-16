namespace LibraryM.Application.Books.Models;

public sealed class BookSearchRequest
{
    public string? Search { get; set; }

    public string? Title { get; set; }

    public string? Author { get; set; }

    public string? Isbn { get; set; }

    public string? Category { get; set; }

    public string? BookType { get; set; }

    public bool AvailableOnly { get; set; }

    public bool ActiveOnly { get; set; } = true;
}
