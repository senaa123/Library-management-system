using LibraryM.Application.Abstractions.Persistence;
using LibraryM.Application.Books.Models;
using LibraryM.Application.Common;
using LibraryM.Domain.Entities;

namespace LibraryM.Application.Books;

public sealed class BookService : IBookService
{
    private readonly IBookRepository _bookRepository;

    public BookService(IBookRepository bookRepository)
    {
        _bookRepository = bookRepository;
    }

    public async Task<IReadOnlyList<BookDto>> GetBooksAsync(string? category, CancellationToken cancellationToken = default)
    {
        var books = await _bookRepository.GetAllAsync(category, cancellationToken);
        return books.Select(Map).ToList();
    }

    public async Task<OperationResult<BookDto>> GetBookByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        var book = await _bookRepository.GetByIdAsync(id, cancellationToken);
        return book is null
            ? OperationResult<BookDto>.Failure("Book not found", FailureType.NotFound)
            : OperationResult<BookDto>.Success(Map(book));
    }

    public async Task<OperationResult<BookDto>> CreateAsync(CreateBookRequest request, CancellationToken cancellationToken = default)
    {
        if (HasMissingBookFields(request.Title, request.Author, request.Description, request.Category))
        {
            return OperationResult<BookDto>.Failure("Title, author, description, and category are required", FailureType.Validation);
        }

        var book = new Book
        {
            Title = request.Title.Trim(),
            Author = request.Author.Trim(),
            Description = request.Description.Trim(),
            Category = request.Category.Trim()
        };

        var createdBook = await _bookRepository.AddAsync(book, cancellationToken);
        return OperationResult<BookDto>.Success(Map(createdBook));
    }

    public async Task<OperationResult> UpdateAsync(int id, UpdateBookRequest request, CancellationToken cancellationToken = default)
    {
        if (!string.IsNullOrWhiteSpace(request.Id) && !string.Equals(request.Id, id.ToString(), StringComparison.Ordinal))
        {
            return OperationResult.Failure("Book ID mismatch", FailureType.Validation);
        }

        if (HasMissingBookFields(request.Title, request.Author, request.Description, request.Category))
        {
            return OperationResult.Failure("Title, author, description, and category are required", FailureType.Validation);
        }

        var book = await _bookRepository.GetByIdAsync(id, cancellationToken);
        if (book is null)
        {
            return OperationResult.Failure("Book not found", FailureType.NotFound);
        }

        book.Title = request.Title.Trim();
        book.Author = request.Author.Trim();
        book.Description = request.Description.Trim();
        book.Category = request.Category.Trim();

        await _bookRepository.UpdateAsync(book, cancellationToken);

        return OperationResult.Success();
    }

    public async Task<OperationResult> DeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        var book = await _bookRepository.GetByIdAsync(id, cancellationToken);
        if (book is null)
        {
            return OperationResult.Failure("Book not found", FailureType.NotFound);
        }

        await _bookRepository.DeleteAsync(book, cancellationToken);
        return OperationResult.Success();
    }

    private static BookDto Map(Book book) => new(book.Id, book.Title, book.Author, book.Description, book.Category);

    private static bool HasMissingBookFields(params string[] values) => values.Any(string.IsNullOrWhiteSpace);
}
