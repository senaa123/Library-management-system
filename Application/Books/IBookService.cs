using LibraryM.Application.Books.Models;
using LibraryM.Application.Common;

namespace LibraryM.Application.Books;

public interface IBookService
{
    Task<IReadOnlyList<BookDto>> GetBooksAsync(string? category, CancellationToken cancellationToken = default);

    Task<OperationResult<BookDto>> GetBookByIdAsync(int id, CancellationToken cancellationToken = default);

    Task<OperationResult<BookDto>> CreateAsync(CreateBookRequest request, CancellationToken cancellationToken = default);

    Task<OperationResult> UpdateAsync(int id, UpdateBookRequest request, CancellationToken cancellationToken = default);

    Task<OperationResult> DeleteAsync(int id, CancellationToken cancellationToken = default);
}
