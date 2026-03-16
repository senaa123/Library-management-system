using LibraryM.Application.Abstractions.Persistence;
using LibraryM.Application.Books.Models;
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

    public async Task<IReadOnlyList<Book>> SearchAsync(BookSearchRequest request, CancellationToken cancellationToken = default)
    {
        var query = _dbContext.Books.AsNoTracking().AsQueryable();

        if (request.ActiveOnly)
        {
            query = query.Where(book => book.IsActive);
        }

        if (request.AvailableOnly)
        {
            query = query.Where(book => book.AvailableCopies > 0);
        }

        if (!string.IsNullOrWhiteSpace(request.Category))
        {
            var category = request.Category.Trim().ToLower();
            query = query.Where(book => book.Category.ToLower() == category);
        }

        if (!string.IsNullOrWhiteSpace(request.BookType))
        {
            var bookType = request.BookType.Trim().ToLower();
            query = query.Where(book => book.BookType.ToLower() == bookType);
        }

        if (!string.IsNullOrWhiteSpace(request.Title))
        {
            var title = request.Title.Trim().ToLower();
            query = query.Where(book => book.Title.ToLower().Contains(title));
        }

        if (!string.IsNullOrWhiteSpace(request.Author))
        {
            var author = request.Author.Trim().ToLower();
            query = query.Where(book => book.Author.ToLower().Contains(author));
        }

        if (!string.IsNullOrWhiteSpace(request.Isbn))
        {
            var isbn = request.Isbn.Trim().ToLower();
            query = query.Where(book => book.Isbn.ToLower() == isbn);
        }

        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var search = request.Search.Trim().ToLower();
            query = query.Where(book =>
                book.Title.ToLower().Contains(search) ||
                book.Author.ToLower().Contains(search) ||
                book.Isbn.ToLower().Contains(search) ||
                book.Category.ToLower().Contains(search) ||
                book.BookType.ToLower().Contains(search));
        }

        return await query.OrderBy(book => book.Title).ToListAsync(cancellationToken);
    }

    public Task<Book?> GetByIdAsync(int id, CancellationToken cancellationToken = default) =>
        _dbContext.Books.FirstOrDefaultAsync(book => book.Id == id, cancellationToken);

    public async Task AddAsync(Book book, CancellationToken cancellationToken = default) =>
        await _dbContext.Books.AddAsync(book, cancellationToken);

    public Task DeleteAsync(Book book, CancellationToken cancellationToken = default)
    {
        _dbContext.Books.Remove(book);
        return Task.CompletedTask;
    }
}
