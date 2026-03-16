namespace LibraryM.Application.Books.Models;

public sealed record BookDto(
    int Id,
    string Title,
    string Author,
    string Description,
    string Category,
    string Isbn,
    string BookType,
    int TotalCopies,
    int AvailableCopies,
    bool IsActive,
    DateTime CreatedAt,
    string AvailabilityStatus);
