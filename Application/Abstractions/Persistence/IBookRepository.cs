using LibraryM.Domain.Entities;

namespace LibraryM.Application.Abstractions.Persistence;

public interface IBookRepository
{
    Task<IReadOnlyList<Book>> GetAllAsync(string? category, CancellationToken cancellationToken = default);

    Task<Book?> GetByIdAsync(int id, CancellationToken cancellationToken = default);

    Task<Book> AddAsync(Book book, CancellationToken cancellationToken = default);

    Task UpdateAsync(Book book, CancellationToken cancellationToken = default);

    Task DeleteAsync(Book book, CancellationToken cancellationToken = default);
}
