using LibraryM.Application.Abstractions.Persistence;
using LibraryM.Domain.Entities;
using LibraryM.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace LibraryM.Infrastructure.Persistence.Repositories;

public sealed class ReservationRepository : IReservationRepository
{
    private readonly LibraryContext _dbContext;

    public ReservationRepository(LibraryContext dbContext)
    {
        _dbContext = dbContext;
    }

    public Task<Reservation?> GetByIdAsync(int id, CancellationToken cancellationToken = default) =>
        BaseQuery().FirstOrDefaultAsync(reservation => reservation.Id == id, cancellationToken);

    public Task<Reservation?> GetActiveReservationAsync(int bookId, int memberId, CancellationToken cancellationToken = default) =>
        BaseQuery().FirstOrDefaultAsync(
            reservation =>
                reservation.BookId == bookId &&
                reservation.MemberId == memberId &&
                (reservation.Status == ReservationStatus.Active || reservation.Status == ReservationStatus.Available),
            cancellationToken);

    public Task<Reservation?> GetNextActiveReservationAsync(int bookId, CancellationToken cancellationToken = default) =>
        BaseQuery()
            .Where(reservation =>
                reservation.BookId == bookId &&
                (reservation.Status == ReservationStatus.Available || reservation.Status == ReservationStatus.Active))
            .OrderBy(reservation => reservation.Status == ReservationStatus.Available ? 0 : 1)
            .ThenBy(reservation => reservation.ReservedAt)
            .FirstOrDefaultAsync(cancellationToken);

    public async Task<IReadOnlyList<Reservation>> GetReservationsAsync(int? memberId, ReservationStatus? status, CancellationToken cancellationToken = default)
    {
        var query = BaseQuery();

        if (memberId.HasValue)
        {
            query = query.Where(reservation => reservation.MemberId == memberId.Value);
        }

        if (status.HasValue)
        {
            query = query.Where(reservation => reservation.Status == status.Value);
        }

        return await query
            .OrderByDescending(reservation => reservation.ReservedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task AddAsync(Reservation reservation, CancellationToken cancellationToken = default) =>
        await _dbContext.Reservations.AddAsync(reservation, cancellationToken);

    private IQueryable<Reservation> BaseQuery() =>
        _dbContext.Reservations
            .Include(reservation => reservation.Book)
            .Include(reservation => reservation.Member);
}
