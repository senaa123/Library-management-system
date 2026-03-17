using LibraryM.Application.Abstractions.Persistence;
using LibraryM.Application.Common;
using LibraryM.Application.Configuration;
using LibraryM.Application.Fines;
using LibraryM.Application.Fines.Models;
using LibraryM.Application.Loans.Models;
using LibraryM.Domain.Entities;
using LibraryM.Domain.Enums;

namespace LibraryM.Application.Loans;

public sealed class LoanService : ILoanService
{
    private readonly IBookRepository _bookRepository;
    private readonly IUserRepository _userRepository;
    private readonly ILoanRepository _loanRepository;
    private readonly IReservationRepository _reservationRepository;
    private readonly ITransactionRepository _transactionRepository;
    private readonly IFineService _fineService;
    private readonly ILibraryUnitOfWork _unitOfWork;
    private readonly LibrarySettings _librarySettings;

    public LoanService(
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

    public async Task<IReadOnlyList<LoanDto>> GetLoansAsync(
        int? memberId,
        bool activeOnly,
        UserRole requesterRole,
        int requesterUserId,
        CancellationToken cancellationToken = default)
    {
        var effectiveMemberId = requesterRole == UserRole.Member ? requesterUserId : memberId;
        var loans = await _loanRepository.GetLoansAsync(effectiveMemberId, activeOnly, cancellationToken);
        return loans.Select(Map).ToList();
    }

    public Task<OperationResult<LoanDto>> BorrowAsync(BorrowBookRequest request, int memberUserId, CancellationToken cancellationToken = default) =>
        Task.FromResult(OperationResult<LoanDto>.Failure(
            "Online borrowing is disabled. Please reserve the book and collect it from the library.",
            FailureType.Conflict));

    public Task<OperationResult<LoanDto>> IssueAsync(IssueLoanRequest request, int issuedByUserId, CancellationToken cancellationToken = default) =>
        IssueInternalAsync(request.BookId, request.MemberId, request.BorrowDays, issuedByUserId, null, consumeAvailableCopy: true, cancellationToken);

    public async Task<OperationResult<LoanDto>> IssueByQrAsync(IssueLoanByQrRequest request, int issuedByUserId, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.MemberQrCodeValue))
        {
            return OperationResult<LoanDto>.Failure("Member QR code is required", FailureType.Validation);
        }

        var member = await _userRepository.GetByQrCodeAsync(request.MemberQrCodeValue, cancellationToken);
        if (member is null)
        {
            return OperationResult<LoanDto>.Failure("No member was found for the scanned QR code", FailureType.NotFound);
        }

        return await IssueInternalAsync(request.BookId, member.Id, request.BorrowDays, issuedByUserId, null, consumeAvailableCopy: true, cancellationToken);
    }

    public async Task<OperationResult<LoanDto>> IssueReservationAsync(int reservationId, int borrowDays, int issuedByUserId, CancellationToken cancellationToken = default)
    {
        var reservation = await _reservationRepository.GetByIdAsync(reservationId, cancellationToken);
        if (reservation is null)
        {
            return OperationResult<LoanDto>.Failure("Reservation not found", FailureType.NotFound);
        }

        if (reservation.Status == ReservationStatus.Active)
        {
            return OperationResult<LoanDto>.Failure("This reservation is still waiting for a copy to become available", FailureType.Conflict);
        }

        if (reservation.Status == ReservationStatus.Cancelled)
        {
            return OperationResult<LoanDto>.Failure("This reservation has already been cancelled", FailureType.Conflict);
        }

        if (reservation.Status == ReservationStatus.Fulfilled)
        {
            return OperationResult<LoanDto>.Failure("This reservation has already been issued", FailureType.Conflict);
        }

        return await IssueInternalAsync(
            reservation.BookId,
            reservation.MemberId,
            borrowDays,
            issuedByUserId,
            reservation,
            consumeAvailableCopy: false,
            cancellationToken);
    }

    private async Task<OperationResult<LoanDto>> IssueInternalAsync(
        int bookId,
        int memberId,
        int borrowDays,
        int issuedByUserId,
        Reservation? reservation,
        bool consumeAvailableCopy,
        CancellationToken cancellationToken)
    {
        if (bookId <= 0 || memberId <= 0)
        {
            return OperationResult<LoanDto>.Failure("Book and member are required", FailureType.Validation);
        }

        if (borrowDays < 1 || borrowDays > _librarySettings.LoanPeriodDays)
        {
            return OperationResult<LoanDto>.Failure($"Borrow period must be between 1 and {_librarySettings.LoanPeriodDays} days", FailureType.Validation);
        }

        var issuedAt = DateTime.UtcNow;
        var book = reservation?.Book ?? await _bookRepository.GetByIdAsync(bookId, cancellationToken);
        if (book is null || !book.IsActive)
        {
            return OperationResult<LoanDto>.Failure("Book not found", FailureType.NotFound);
        }

        if (consumeAvailableCopy)
        {
            var promotedReservations = await PromoteQueuedReservationsAsync(book, issuedByUserId, issuedAt, cancellationToken);

            if (book.AvailableCopies <= 0)
            {
                if (promotedReservations > 0)
                {
                    await _unitOfWork.SaveChangesAsync(cancellationToken);
                    return OperationResult<LoanDto>.Failure("This book is currently reserved for pickup", FailureType.Conflict);
                }

                var blockingReservation = await _reservationRepository.GetNextActiveReservationAsync(book.Id, cancellationToken);
                return blockingReservation?.Status == ReservationStatus.Available
                    ? OperationResult<LoanDto>.Failure("This book is currently reserved for pickup", FailureType.Conflict)
                    : OperationResult<LoanDto>.Failure("No copies are currently available", FailureType.Conflict);
            }
        }
        else if (reservation?.Status != ReservationStatus.Available)
        {
            return OperationResult<LoanDto>.Failure("This reservation is not ready for issue", FailureType.Conflict);
        }

        var member = reservation?.Member ?? await _userRepository.GetByIdAsync(memberId, cancellationToken);
        if (member is null || member.Role != UserRole.Member)
        {
            return OperationResult<LoanDto>.Failure("Member account not found", FailureType.NotFound);
        }

        if (!member.IsActive)
        {
            return OperationResult<LoanDto>.Failure("Member account is inactive", FailureType.Conflict);
        }

        var fineStatus = await _fineService.GetMemberStatusAsync(member.Id, cancellationToken);
        if (fineStatus.IsCirculationBlocked)
        {
            return OperationResult<LoanDto>.Failure(
                string.IsNullOrWhiteSpace(fineStatus.WarningMessage)
                    ? "This member is currently restricted from borrowing books."
                    : fineStatus.WarningMessage,
                FailureType.Conflict);
        }

        var activeLoans = await _loanRepository.GetLoansAsync(member.Id, activeOnly: true, cancellationToken);
        var activeReservations = await _reservationRepository.GetReservationsAsync(member.Id, null, cancellationToken);
        var currentCirculationCount = activeLoans.Count + activeReservations.Count(currentReservation =>
            currentReservation.Status is ReservationStatus.Active or ReservationStatus.Available);
        var projectedCirculationCount = reservation is null ? currentCirculationCount + 1 : currentCirculationCount;

        if (projectedCirculationCount > fineStatus.MaxCirculationItems)
        {
            return OperationResult<LoanDto>.Failure(
                $"This member is limited to {fineStatus.MaxCirculationItems} active reserved/borrowed books right now.",
                FailureType.Conflict);
        }

        var existingLoan = await _loanRepository.GetActiveLoanAsync(book.Id, member.Id, cancellationToken);
        if (existingLoan is not null)
        {
            return OperationResult<LoanDto>.Failure("This member already has an active loan for the selected book", FailureType.Conflict);
        }

        if (reservation is null)
        {
            var existingReservation = await _reservationRepository.GetActiveReservationAsync(book.Id, member.Id, cancellationToken);
            if (existingReservation is not null)
            {
                return OperationResult<LoanDto>.Failure("This member already has a reservation for the selected book", FailureType.Conflict);
            }
        }

        var loan = new Loan
        {
            BookId = book.Id,
            Book = book,
            BorrowerId = member.Id,
            Borrower = member,
            IssuedById = issuedByUserId,
            IssuedAt = issuedAt,
            DueDate = issuedAt.AddDays(borrowDays),
            RenewCount = 0,
            Status = LoanStatus.Active
        };

        if (consumeAvailableCopy)
        {
            book.AvailableCopies -= 1;
        }

        if (reservation is not null)
        {
            reservation.Status = ReservationStatus.Fulfilled;
            reservation.FulfilledAt = issuedAt;
        }

        await _loanRepository.AddAsync(loan, cancellationToken);
        await _transactionRepository.AddAsync(
            new TransactionRecord
            {
                Type = TransactionType.Issue,
                BookId = book.Id,
                UserId = member.Id,
                PerformedById = issuedByUserId,
                Loan = loan,
                ReservationId = reservation?.Id,
                Details = reservation is null
                    ? $"Book '{book.Title}' was issued to '{member.Username}' after QR verification."
                    : $"Reserved book '{book.Title}' was issued to '{member.Username}'."
            },
            cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return OperationResult<LoanDto>.Success(Map(loan));
    }

    public async Task<OperationResult<LoanDto>> ReturnAsync(
        int loanId,
        ReturnLoanRequest request,
        int returnedByUserId,
        CancellationToken cancellationToken = default)
    {
        var loan = await _loanRepository.GetByIdAsync(loanId, cancellationToken);
        if (loan is null)
        {
            return OperationResult<LoanDto>.Failure("Loan not found", FailureType.NotFound);
        }

        if (loan.Status != LoanStatus.Active)
        {
            return OperationResult<LoanDto>.Failure("Only active loans can be returned", FailureType.Conflict);
        }

        if (loan.Book is null)
        {
            return OperationResult<LoanDto>.Failure("Loan book details are unavailable", FailureType.Conflict);
        }

        var fineType = request.AddFine ? ParseFineType(request.FineType) : null;
        if (request.AddFine && fineType is null)
        {
            return OperationResult<LoanDto>.Failure("A valid fine type is required when adding a condition fine.", FailureType.Validation);
        }

        var returnedAt = DateTime.UtcNow;
        loan.Status = LoanStatus.Returned;
        loan.ReturnedAt = returnedAt;

        if (fineType == FineChargeType.LostBook)
        {
            // Lost copies should not come back into live stock, so we reduce the catalog total instead.
            loan.Book.TotalCopies = Math.Max(0, loan.Book.TotalCopies - 1);
            loan.Book.AvailableCopies = Math.Min(loan.Book.AvailableCopies, loan.Book.TotalCopies);
        }
        else
        {
            loan.Book.AvailableCopies = Math.Min(loan.Book.TotalCopies, loan.Book.AvailableCopies + 1);

            // When a copy comes back, we immediately hold it for the next queued reservation before exposing it as free stock.
            await PromoteQueuedReservationsAsync(loan.Book, returnedByUserId, returnedAt, cancellationToken);
        }

        await _transactionRepository.AddAsync(
            new TransactionRecord
            {
                Type = TransactionType.Return,
                BookId = loan.BookId,
                UserId = loan.BorrowerId,
                PerformedById = returnedByUserId,
                LoanId = loan.Id,
                Details = $"Book '{loan.Book.Title}' was returned by '{loan.Borrower?.Username ?? "member"}'."
            },
            cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        if (fineType.HasValue)
        {
            var chargeAmount = ResolveFineAmount(fineType.Value);
            var description = fineType.Value switch
            {
                FineChargeType.DamagedBook => $"Condition fine added when '{loan.Book.Title}' was returned damaged.",
                FineChargeType.LostBook => $"Replacement fine added because '{loan.Book.Title}' was reported lost.",
                FineChargeType.MissingPages => $"Condition fine added because '{loan.Book.Title}' was returned with missing pages.",
                _ => string.Empty
            };

            await _fineService.AddChargeAsync(
                new CreateFineChargeRequest(
                    loan.BorrowerId,
                    fineType.Value,
                    chargeAmount,
                    description,
                    returnedByUserId,
                    loan.Id),
                cancellationToken);
        }

        return OperationResult<LoanDto>.Success(Map(loan));
    }

    public async Task<OperationResult<LoanDto>> RenewAsync(
        int loanId,
        int requesterUserId,
        UserRole requesterRole,
        CancellationToken cancellationToken = default)
    {
        var loan = await _loanRepository.GetByIdAsync(loanId, cancellationToken);
        if (loan is null)
        {
            return OperationResult<LoanDto>.Failure("Loan not found", FailureType.NotFound);
        }

        if (loan.Status != LoanStatus.Active)
        {
            return OperationResult<LoanDto>.Failure("Only active loans can be renewed", FailureType.Conflict);
        }

        if (requesterRole == UserRole.Member && loan.BorrowerId != requesterUserId)
        {
            return OperationResult<LoanDto>.Failure("You can only renew your own loans", FailureType.Forbidden);
        }

        if (loan.RenewCount >= _librarySettings.MaxRenewals)
        {
            return OperationResult<LoanDto>.Failure("This loan has reached its renewal limit", FailureType.Conflict);
        }

        var queuedReservation = await _reservationRepository.GetNextActiveReservationAsync(loan.BookId, cancellationToken);
        if (queuedReservation is not null && queuedReservation.MemberId != loan.BorrowerId)
        {
            return OperationResult<LoanDto>.Failure("This loan cannot be renewed because another member is waiting", FailureType.Conflict);
        }

        loan.DueDate = loan.DueDate.AddDays(_librarySettings.RenewalDays);
        loan.RenewCount += 1;

        await _transactionRepository.AddAsync(
            new TransactionRecord
            {
                Type = TransactionType.Renew,
                BookId = loan.BookId,
                UserId = loan.BorrowerId,
                PerformedById = requesterUserId,
                LoanId = loan.Id,
                Details = $"Loan #{loan.Id} was renewed."
            },
            cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return OperationResult<LoanDto>.Success(Map(loan));
    }

    private async Task<int> PromoteQueuedReservationsAsync(Book book, int? performedByUserId, DateTime currentTime, CancellationToken cancellationToken)
    {
        var promotedCount = 0;

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
            promotedCount++;
        }

        return promotedCount;
    }

    private LoanDto Map(Loan loan)
    {
        var timeMetrics = CalculateTimeLeft(loan);

        return new LoanDto(
            loan.Id,
            loan.BookId,
            loan.Book?.Title ?? string.Empty,
            loan.Book?.Isbn ?? string.Empty,
            loan.BorrowerId,
            loan.Borrower?.FullName ?? string.Empty,
            loan.Borrower?.Username ?? string.Empty,
            loan.Borrower?.PhoneNumber ?? string.Empty,
            loan.IssuedById,
            loan.IssuedBy?.FullName ?? string.Empty,
            loan.IssuedAt,
            loan.DueDate,
            loan.ReturnedAt,
            loan.RenewCount,
            loan.Status.ToString(),
            FineCalculator.CalculateAccruedFine(loan, _librarySettings, DateTime.UtcNow),
            Math.Max(1, (loan.DueDate.Date - loan.IssuedAt.Date).Days),
            timeMetrics.daysLeft,
            timeMetrics.label);
    }

    private static (int daysLeft, string label) CalculateTimeLeft(Loan loan)
    {
        var effectiveEnd = loan.Status == LoanStatus.Returned && loan.ReturnedAt.HasValue
            ? loan.ReturnedAt.Value.Date
            : DateTime.UtcNow.Date;
        var daysLeft = (loan.DueDate.Date - effectiveEnd).Days;

        if (loan.Status == LoanStatus.Returned)
        {
            return (daysLeft, "Returned");
        }

        if (daysLeft > 1)
        {
            return (daysLeft, $"{daysLeft} days left");
        }

        if (daysLeft == 1)
        {
            return (daysLeft, "1 day left");
        }

        if (daysLeft == 0)
        {
            return (daysLeft, "Due today");
        }

        var overdueDays = Math.Abs(daysLeft);
        return (daysLeft, overdueDays == 1 ? "1 day overdue" : $"{overdueDays} days overdue");
    }

    private FineChargeType? ParseFineType(string? fineType)
    {
        if (string.IsNullOrWhiteSpace(fineType))
        {
            return null;
        }

        return Enum.TryParse<FineChargeType>(fineType.Trim(), ignoreCase: true, out var parsedType)
            ? parsedType
            : null;
    }

    private decimal ResolveFineAmount(FineChargeType fineType) =>
        fineType switch
        {
            FineChargeType.DamagedBook => _librarySettings.DamagedBookFine,
            FineChargeType.LostBook => _librarySettings.LostBookFine,
            FineChargeType.MissingPages => _librarySettings.MissingPagesFine,
            _ => 0m
        };
}
