using LibraryM.Domain.Entities;
using LibraryM.Domain.Enums;

namespace LibraryM.Application.Abstractions.Persistence;

public interface IReservationRepository
{
    Task<Reservation?> GetByIdAsync(int id, CancellationToken cancellationToken = default);

    Task<Reservation?> GetActiveReservationAsync(int bookId, int memberId, CancellationToken cancellationToken = default);

    Task<Reservation?> GetNextActiveReservationAsync(int bookId, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Reservation>> GetReservationsAsync(int? memberId, ReservationStatus? status, CancellationToken cancellationToken = default);

    Task AddAsync(Reservation reservation, CancellationToken cancellationToken = default);
}
