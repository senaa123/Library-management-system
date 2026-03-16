using LibraryM.Application.Abstractions.Persistence;

namespace LibraryM.Infrastructure.Persistence;

public sealed class LibraryUnitOfWork : ILibraryUnitOfWork
{
    private readonly LibraryContext _dbContext;

    public LibraryUnitOfWork(LibraryContext dbContext)
    {
        _dbContext = dbContext;
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken = default) =>
        _dbContext.SaveChangesAsync(cancellationToken);
}
