using LibraryM.Application.Abstractions.Persistence;
using LibraryM.Application.Common;
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
    private readonly ILibraryUnitOfWork _unitOfWork;

    public ReservationService(
        IBookRepository bookRepository,
        IUserRepository userRepository,
        ILoanRepository loanRepository,
        IReservationRepository reservationRepository,
        ITransactionRepository transactionRepository,
        ILibraryUnitOfWork unitOfWork)
    {
        _bookRepository = bookRepository;
        _userRepository = userRepository;
        _loanRepository = loanRepository;
        _reservationRepository = reservationRepository;
        _transactionRepository = transactionRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<IReadOnlyList<ReservationDto>> GetReservationsAsync(
        int? memberId,
        ReservationStatus? status,
        UserRole requesterRole,
        int requesterUserId,
        CancellationToken cancellationToken = default)
    {
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

        if (book.AvailableCopies > 0)
        {
            return OperationResult<ReservationDto>.Failure("This book is available right now and does not need a reservation", FailureType.Conflict);
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

        var reservedAt = DateTime.UtcNow;
        var reservation = new Reservation
        {
            BookId = book.Id,
            Book = book,
            MemberId = member.Id,
            Member = member,
            ReservedAt = reservedAt,
            Status = ReservationStatus.Active
        };

        await _reservationRepository.AddAsync(reservation, cancellationToken);
        await _transactionRepository.AddAsync(
            new TransactionRecord
            {
                Type = TransactionType.ReservationPlaced,
                BookId = book.Id,
                UserId = member.Id,
                PerformedById = requesterUserId,
                Reservation = reservation,
                Details = $"Reservation placed for '{book.Title}' by '{member.Username}'."
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

        reservation.Status = ReservationStatus.Cancelled;
        reservation.CancelledAt = DateTime.UtcNow;

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

    private static ReservationDto Map(Reservation reservation) =>
        new(
            reservation.Id,
            reservation.BookId,
            reservation.Book?.Title ?? string.Empty,
            reservation.MemberId,
            reservation.Member?.FullName ?? string.Empty,
            reservation.Member?.Username ?? string.Empty,
            reservation.ReservedAt,
            reservation.NotifiedAt,
            reservation.CancelledAt,
            reservation.FulfilledAt,
            reservation.Status.ToString());
}
