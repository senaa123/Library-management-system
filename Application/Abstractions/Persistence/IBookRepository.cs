using LibraryM.Application.Books.Models;
using LibraryM.Domain.Entities;

namespace LibraryM.Application.Abstractions.Persistence;

public interface IBookRepository
{
    Task<IReadOnlyList<Book>> SearchAsync(BookSearchRequest request, CancellationToken cancellationToken = default);

    Task<Book?> GetByIdAsync(int id, CancellationToken cancellationToken = default);

    Task AddAsync(Book book, CancellationToken cancellationToken = default);

    Task DeleteAsync(Book book, CancellationToken cancellationToken = default);
}
