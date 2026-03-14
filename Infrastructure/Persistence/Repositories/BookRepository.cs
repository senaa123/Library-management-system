using LibraryM.Application.Abstractions.Persistence;
using LibraryM.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace LibraryM.Infrastructure.Persistence.Repositories;

public sealed class BookRepository : IBookRepository
{
    private readonly LibraryContext _dbContext;

    public BookRepository(LibraryContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<IReadOnlyList<Book>> GetAllAsync(string? category, CancellationToken cancellationToken = default)
    {
        var query = _dbContext.Books.AsNoTracking().AsQueryable();

        if (!string.IsNullOrWhiteSpace(category))
        {
            query = query.Where(book => book.Category == category);
        }

        return await query.ToListAsync(cancellationToken);
    }

    public Task<Book?> GetByIdAsync(int id, CancellationToken cancellationToken = default) =>
        _dbContext.Books.FirstOrDefaultAsync(book => book.Id == id, cancellationToken);

    public async Task<Book> AddAsync(Book book, CancellationToken cancellationToken = default)
    {
        _dbContext.Books.Add(book);
        await _dbContext.SaveChangesAsync(cancellationToken);
        return book;
    }

    public async Task UpdateAsync(Book book, CancellationToken cancellationToken = default)
    {
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task DeleteAsync(Book book, CancellationToken cancellationToken = default)
    {
        _dbContext.Books.Remove(book);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }
}
