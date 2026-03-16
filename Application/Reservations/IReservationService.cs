using LibraryM.Application.Common;
using LibraryM.Application.Reservations.Models;
using LibraryM.Domain.Enums;

namespace LibraryM.Application.Reservations;

public interface IReservationService
{
    Task<IReadOnlyList<ReservationDto>> GetReservationsAsync(int? memberId, ReservationStatus? status, UserRole requesterRole, int requesterUserId, CancellationToken cancellationToken = default);

    Task<OperationResult<ReservationDto>> CreateAsync(CreateReservationRequest request, int requesterUserId, UserRole requesterRole, CancellationToken cancellationToken = default);

    Task<OperationResult<ReservationDto>> CancelAsync(int reservationId, int requesterUserId, UserRole requesterRole, CancellationToken cancellationToken = default);
}
