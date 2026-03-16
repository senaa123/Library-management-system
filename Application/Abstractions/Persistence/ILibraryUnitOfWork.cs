namespace LibraryM.Application.Abstractions.Persistence;

public interface ILibraryUnitOfWork
{
    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
