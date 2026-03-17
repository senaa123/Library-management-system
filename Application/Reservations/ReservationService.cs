using LibraryM.Application.Abstractions.Persistence;
using LibraryM.Application.Common;
using LibraryM.Application.Configuration;
using LibraryM.Application.Fines;
using LibraryM.Application.Fines.Models;
using LibraryM.Application.Reservations.Models;
using LibraryM.Domain.Entities;
using LibraryM.Domain.Enums;

namespace LibraryM.Application.Reservations;

public sealed class ReservationService : IReservationService
{
    private readonly IBookRepository _bookRepository;
    private readonly IUserRepository _userRepository;
    private readonly ILoanRepository _loanRepository;
    private readonly IReservationRepository _reservationRepository;
    private readonly ITransactionRepository _transactionRepository;
    private readonly IFineService _fineService;
    private readonly ILibraryUnitOfWork _unitOfWork;
    private readonly LibrarySettings _librarySettings;

    public ReservationService(
        IBookRepository bookRepository,
        IUserRepository userRepository,
        ILoanRepository loanRepository,
        IReservationRepository reservationRepository,
        ITransactionRepository transactionRepository,
        IFineService fineService,
        ILibraryUnitOfWork unitOfWork,
        LibrarySettings librarySettings)
    {
        _bookRepository = bookRepository;
        _userRepository = userRepository;
        _loanRepository = loanRepository;
        _reservationRepository = reservationRepository;
        _transactionRepository = transactionRepository;
        _fineService = fineService;
        _unitOfWork = unitOfWork;
        _librarySettings = librarySettings;
    }

    public async Task<IReadOnlyList<ReservationDto>> GetReservationsAsync(
        int? memberId,
        ReservationStatus? status,
        UserRole requesterRole,
        int requesterUserId,
        CancellationToken cancellationToken = default)
    {
        await ExpireExpiredAsync(cancellationToken);

        var effectiveMemberId = requesterRole == UserRole.Member ? requesterUserId : memberId;
        var reservations = await _reservationRepository.GetReservationsAsync(effectiveMemberId, status, cancellationToken);
        return reservations.Select(Map).ToList();
    }

    public async Task<OperationResult<ReservationDto>> CreateAsync(
        CreateReservationRequest request,
        int requesterUserId,
        UserRole requesterRole,
        CancellationToken cancellationToken = default)
    {
        if (request.BookId <= 0)
        {
            return OperationResult<ReservationDto>.Failure("Book is required", FailureType.Validation);
        }

        await ExpireExpiredAsync(cancellationToken);

        var memberId = requesterRole == UserRole.Member ? requesterUserId : request.MemberId.GetValueOrDefault();
        if (memberId <= 0)
        {
            return OperationResult<ReservationDto>.Failure("Member is required", FailureType.Validation);
        }

        var book = await _bookRepository.GetByIdAsync(request.BookId, cancellationToken);
        if (book is null || !book.IsActive)
        {
            return OperationResult<ReservationDto>.Failure("Book not found", FailureType.NotFound);
        }

        var member = await _userRepository.GetByIdAsync(memberId, cancellationToken);
        if (member is null || member.Role != UserRole.Member)
        {
            return OperationResult<ReservationDto>.Failure("Member account not found", FailureType.NotFound);
        }

        if (!member.IsActive)
        {
            return OperationResult<ReservationDto>.Failure("Member account is inactive", FailureType.Conflict);
        }

        var fineStatus = await _fineService.GetMemberStatusAsync(member.Id, cancellationToken);
        if (fineStatus.IsCirculationBlocked)
        {
            return OperationResult<ReservationDto>.Failure(
                string.IsNullOrWhiteSpace(fineStatus.WarningMessage)
                    ? "This member is currently restricted from placing reservations."
                    : fineStatus.WarningMessage,
                FailureType.Conflict);
        }

        var existingReservation = await _reservationRepository.GetActiveReservationAsync(book.Id, member.Id, cancellationToken);
        if (existingReservation is not null)
        {
            return OperationResult<ReservationDto>.Failure("This member already has an active reservation for the selected book", FailureType.Conflict);
        }

        var activeLoan = await _loanRepository.GetActiveLoanAsync(book.Id, member.Id, cancellationToken);
        if (activeLoan is not null)
        {
            return OperationResult<ReservationDto>.Failure("The member already has this book on loan", FailureType.Conflict);
        }

        var activeLoans = await _loanRepository.GetLoansAsync(member.Id, activeOnly: true, cancellationToken);
        var activeReservations = await _reservationRepository.GetReservationsAsync(member.Id, null, cancellationToken);
        var currentCirculationCount = activeLoans.Count + activeReservations.Count(currentReservation =>
            currentReservation.Status is ReservationStatus.Active or ReservationStatus.Available);

        if (currentCirculationCount >= fineStatus.MaxCirculationItems)
        {
            return OperationResult<ReservationDto>.Failure(
                $"This member is limited to {fineStatus.MaxCirculationItems} active reserved/borrowed books right now.",
                FailureType.Conflict);
        }

        var reservedAt = DateTime.UtcNow;
        await PromoteQueuedReservationsAsync(book, requesterUserId, reservedAt, cancellationToken);

        var reservation = new Reservation
        {
            BookId = book.Id,
            Book = book,
            MemberId = member.Id,
            Member = member,
            ReservedAt = reservedAt,
            Status = ReservationStatus.Active
        };

        // If a copy is free right now, we immediately hold it for pickup and start the five-day collection window.
        if (book.AvailableCopies > 0)
        {
            reservation.Status = ReservationStatus.Available;
            reservation.NotifiedAt = reservedAt;
            book.AvailableCopies -= 1;
        }

        await _reservationRepository.AddAsync(reservation, cancellationToken);
        await _transactionRepository.AddAsync(
            new TransactionRecord
            {
                Type = TransactionType.ReservationPlaced,
                BookId = book.Id,
                UserId = member.Id,
                PerformedById = requesterUserId,
                Reservation = reservation,
                Details = reservation.Status == ReservationStatus.Available
                    ? $"Reservation placed for '{book.Title}'. A copy is held for '{member.Username}' until pickup."
                    : $"Reservation placed for '{book.Title}' by '{member.Username}' and added to the waiting queue."
            },
            cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return OperationResult<ReservationDto>.Success(Map(reservation));
    }

    public async Task<OperationResult<ReservationDto>> CancelAsync(
        int reservationId,
        int requesterUserId,
        UserRole requesterRole,
        CancellationToken cancellationToken = default)
    {
        var reservation = await _reservationRepository.GetByIdAsync(reservationId, cancellationToken);
        if (reservation is null)
        {
            return OperationResult<ReservationDto>.Failure("Reservation not found", FailureType.NotFound);
        }

        if (requesterRole == UserRole.Member && reservation.MemberId != requesterUserId)
        {
            return OperationResult<ReservationDto>.Failure("You can only cancel your own reservations", FailureType.Forbidden);
        }

        if (reservation.Status is ReservationStatus.Cancelled or ReservationStatus.Fulfilled)
        {
            return OperationResult<ReservationDto>.Failure("This reservation can no longer be cancelled", FailureType.Conflict);
        }

        var cancelledAt = DateTime.UtcNow;
        var wasReadyForPickup = reservation.Status == ReservationStatus.Available;
        reservation.Status = ReservationStatus.Cancelled;
        reservation.CancelledAt = cancelledAt;

        if (wasReadyForPickup && reservation.Book is not null)
        {
            reservation.Book.AvailableCopies = Math.Min(reservation.Book.TotalCopies, reservation.Book.AvailableCopies + 1);
            await PromoteQueuedReservationsAsync(reservation.Book, requesterUserId, cancelledAt, cancellationToken);
        }

        await _transactionRepository.AddAsync(
            new TransactionRecord
            {
                Type = TransactionType.ReservationCancelled,
                BookId = reservation.BookId,
                UserId = reservation.MemberId,
                PerformedById = requesterUserId,
                ReservationId = reservation.Id,
                Details = $"Reservation #{reservation.Id} was cancelled."
            },
            cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return OperationResult<ReservationDto>.Success(Map(reservation));
    }

    public async Task<int> ExpireExpiredAsync(CancellationToken cancellationToken = default)
    {
        var expiresBeforeUtc = DateTime.UtcNow.AddDays(-_librarySettings.ReservationHoldDays);
        var expiredReservations = await _reservationRepository.GetExpiredReadyReservationsAsync(expiresBeforeUtc, cancellationToken);
        if (expiredReservations.Count == 0)
        {
            return 0;
        }

        var currentTime = DateTime.UtcNow;
        var expiredCount = 0;

        foreach (var reservation in expiredReservations)
        {
            if (reservation.Status != ReservationStatus.Available)
            {
                continue;
            }

            reservation.Status = ReservationStatus.Cancelled;
            reservation.CancelledAt = currentTime;

            if (reservation.Book is not null)
            {
                reservation.Book.AvailableCopies = Math.Min(reservation.Book.TotalCopies, reservation.Book.AvailableCopies + 1);
                await PromoteQueuedReservationsAsync(reservation.Book, null, currentTime, cancellationToken);
            }

            await _fineService.AddChargeAsync(
                new CreateFineChargeRequest(
                    reservation.MemberId,
                    FineChargeType.ReservationNoShow,
                    _librarySettings.ReservationNoShowFine,
                    $"Pickup window expired for reservation #{reservation.Id}.",
                    null,
                    null,
                    reservation.Id,
                    $"reservation-expired-{reservation.Id}"),
                cancellationToken);

            await _transactionRepository.AddAsync(
                new TransactionRecord
                {
                    Type = TransactionType.ReservationExpired,
                    BookId = reservation.BookId,
                    UserId = reservation.MemberId,
                    ReservationId = reservation.Id,
                    Details = $"Reservation #{reservation.Id} expired because the pickup window elapsed."
                },
                cancellationToken);
            expiredCount++;
        }

        if (expiredCount > 0)
        {
            await _unitOfWork.SaveChangesAsync(cancellationToken);
        }

        return expiredCount;
    }

    private async Task PromoteQueuedReservationsAsync(Book book, int? performedByUserId, DateTime currentTime, CancellationToken cancellationToken)
    {
        // Every free copy should be assigned to the next queued reservation before staff can issue it manually.
        while (book.AvailableCopies > 0)
        {
            var nextQueuedReservation = await _reservationRepository.GetNextQueuedReservationAsync(book.Id, cancellationToken);
            if (nextQueuedReservation is null)
            {
                break;
            }

            nextQueuedReservation.Status = ReservationStatus.Available;
            nextQueuedReservation.NotifiedAt = currentTime;
            book.AvailableCopies -= 1;

            await _transactionRepository.AddAsync(
                new TransactionRecord
                {
                    Type = TransactionType.ReservationAvailable,
                    BookId = nextQueuedReservation.BookId,
                    UserId = nextQueuedReservation.MemberId,
                    PerformedById = performedByUserId,
                    ReservationId = nextQueuedReservation.Id,
                    Details = $"Reservation #{nextQueuedReservation.Id} is ready for pickup."
                },
                cancellationToken);
        }
    }

    private ReservationDto Map(Reservation reservation)
    {
        var pickupWindow = CalculatePickupWindow(reservation);

        return new ReservationDto(
            reservation.Id,
            reservation.BookId,
            reservation.Book?.Title ?? string.Empty,
            reservation.MemberId,
            reservation.Member?.FullName ?? string.Empty,
            reservation.Member?.Username ?? string.Empty,
            reservation.Member?.PhoneNumber ?? string.Empty,
            reservation.ReservedAt,
            reservation.NotifiedAt,
            reservation.CancelledAt,
            reservation.FulfilledAt,
            pickupWindow.deadline,
            pickupWindow.daysLeft,
            pickupWindow.label,
            reservation.Status == ReservationStatus.Available,
            reservation.Status.ToString());
    }

    private (DateTime? deadline, int daysLeft, string label) CalculatePickupWindow(Reservation reservation)
    {
        if (reservation.Status == ReservationStatus.Active)
        {
            return (null, 0, "Waiting for a copy");
        }

        if (reservation.Status == ReservationStatus.Cancelled)
        {
            return (null, 0, "Cancelled");
        }

        if (reservation.Status == ReservationStatus.Fulfilled)
        {
            return (null, 0, "Issued");
        }

        var readyAt = reservation.NotifiedAt ?? reservation.ReservedAt;
        var deadline = readyAt.AddDays(_librarySettings.ReservationHoldDays);
        var daysLeft = (deadline.Date - DateTime.UtcNow.Date).Days;

        if (daysLeft > 1)
        {
            return (deadline, daysLeft, $"{daysLeft} days left to collect");
        }

        if (daysLeft == 1)
        {
            return (deadline, daysLeft, "1 day left to collect");
        }

        if (daysLeft == 0)
        {
            return (deadline, daysLeft, "Collect today");
        }

        var overdueDays = Math.Abs(daysLeft);
        return (deadline, daysLeft, overdueDays == 1 ? "1 day overdue for pickup" : $"{overdueDays} days overdue for pickup");
    }
}
