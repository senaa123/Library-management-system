using LibraryM.Application.Abstractions.Persistence;
using LibraryM.Application.Books.Models;
using LibraryM.Application.Common;
using LibraryM.Domain.Entities;
using LibraryM.Domain.Enums;

namespace LibraryM.Application.Books;

public sealed class BookService : IBookService
{
    private readonly IBookRepository _bookRepository;
    private readonly ITransactionRepository _transactionRepository;
    private readonly ILibraryUnitOfWork _unitOfWork;

    public BookService(
        IBookRepository bookRepository,
        ITransactionRepository transactionRepository,
        ILibraryUnitOfWork unitOfWork)
    {
        _bookRepository = bookRepository;
        _transactionRepository = transactionRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<IReadOnlyList<BookDto>> SearchAsync(BookSearchRequest request, CancellationToken cancellationToken = default)
    {
        var books = await _bookRepository.SearchAsync(request, cancellationToken);
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

        var totalCopies = request.TotalCopies.GetValueOrDefault(1);
        var availableCopies = request.AvailableCopies.GetValueOrDefault(totalCopies);
        if (totalCopies < 1 || availableCopies < 0 || availableCopies > totalCopies)
        {
            return OperationResult<BookDto>.Failure("Book copy counts are invalid", FailureType.Validation);
        }

        var book = new Book
        {
            Title = request.Title.Trim(),
            Author = request.Author.Trim(),
            Description = request.Description.Trim(),
            Category = request.Category.Trim(),
            Isbn = request.Isbn?.Trim() ?? string.Empty,
            BookType = string.IsNullOrWhiteSpace(request.BookType) ? "General" : request.BookType.Trim(),
            TotalCopies = totalCopies,
            AvailableCopies = availableCopies,
            IsActive = request.IsActive,
            CreatedAt = DateTime.UtcNow
        };

        await _bookRepository.AddAsync(book, cancellationToken);
        await _transactionRepository.AddAsync(
            new TransactionRecord
            {
                Type = TransactionType.BookAdded,
                Book = book,
                Details = $"Book '{book.Title}' was added to the catalog."
            },
            cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return OperationResult<BookDto>.Success(Map(book));
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

        var borrowedCopies = Math.Max(0, book.TotalCopies - book.AvailableCopies);
        var totalCopies = request.TotalCopies ?? book.TotalCopies;
        if (totalCopies < 1 || totalCopies < borrowedCopies)
        {
            return OperationResult.Failure("Total copies cannot be less than the number of issued copies", FailureType.Validation);
        }

        var availableCopies = request.AvailableCopies ?? Math.Max(0, totalCopies - borrowedCopies);
        if (availableCopies < 0 || availableCopies > totalCopies)
        {
            return OperationResult.Failure("Available copies are invalid", FailureType.Validation);
        }

        if (totalCopies - availableCopies < borrowedCopies)
        {
            return OperationResult.Failure("Available copies cannot conflict with active loans", FailureType.Validation);
        }

        book.Title = request.Title.Trim();
        book.Author = request.Author.Trim();
        book.Description = request.Description.Trim();
        book.Category = request.Category.Trim();
        book.Isbn = request.Isbn?.Trim() ?? string.Empty;
        book.BookType = string.IsNullOrWhiteSpace(request.BookType) ? "General" : request.BookType.Trim();
        book.TotalCopies = totalCopies;
        book.AvailableCopies = availableCopies;
        book.IsActive = request.IsActive ?? book.IsActive;

        await _transactionRepository.AddAsync(
            new TransactionRecord
            {
                Type = TransactionType.BookUpdated,
                BookId = book.Id,
                Details = $"Book '{book.Title}' was updated."
            },
            cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return OperationResult.Success();
    }

    public async Task<OperationResult> RemoveAsync(int id, RemoveBookRequest request, CancellationToken cancellationToken = default)
    {
        var book = await _bookRepository.GetByIdAsync(id, cancellationToken);
        if (book is null)
        {
            return OperationResult.Failure("Book not found", FailureType.NotFound);
        }

        var borrowedCopies = Math.Max(0, book.TotalCopies - book.AvailableCopies);

        if (request.RemoveAllCopies)
        {
            // We only allow removing the whole title when nothing is still out with members.
            if (borrowedCopies > 0)
            {
                return OperationResult.Failure("All copies cannot be removed while some copies are still borrowed", FailureType.Conflict);
            }

            book.TotalCopies = 0;
            book.AvailableCopies = 0;
            book.IsActive = false;

            await _transactionRepository.AddAsync(
                new TransactionRecord
                {
                    Type = TransactionType.BookDeleted,
                    BookId = book.Id,
                    Details = $"All copies of '{book.Title}' were removed from the library."
                },
                cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            return OperationResult.Success();
        }

        var quantityToRemove = request.QuantityToRemove.GetValueOrDefault();
        if (quantityToRemove <= 0)
        {
            return OperationResult.Failure("A valid quantity is required", FailureType.Validation);
        }

        // Librarians can only remove copies that are physically available in the library.
        if (quantityToRemove > book.AvailableCopies)
        {
            return OperationResult.Failure("You cannot remove more copies than are currently available in the library", FailureType.Conflict);
        }

        book.TotalCopies -= quantityToRemove;
        book.AvailableCopies -= quantityToRemove;

        if (book.TotalCopies == 0)
        {
            book.IsActive = false;
        }

        await _transactionRepository.AddAsync(
            new TransactionRecord
            {
                Type = TransactionType.BookDeleted,
                BookId = book.Id,
                Details = $"{quantityToRemove} copies of '{book.Title}' were removed from the library."
            },
            cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return OperationResult.Success();
    }

    private static BookDto Map(Book book) =>
        new(
            book.Id,
            book.Title,
            book.Author,
            book.Description,
            book.Category,
            book.Isbn,
            book.BookType,
            book.TotalCopies,
            book.AvailableCopies,
            book.IsActive,
            book.CreatedAt,
            book.AvailableCopies > 0 ? "Available" : "Not available at the moment");

    private static bool HasMissingBookFields(params string[] values) => values.Any(string.IsNullOrWhiteSpace);
}
