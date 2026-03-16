using LibraryM.Application.Abstractions.Persistence;
using LibraryM.Application.Common;
using LibraryM.Application.Configuration;
using LibraryM.Application.Fines;
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
    private readonly ILibraryUnitOfWork _unitOfWork;
    private readonly LibrarySettings _librarySettings;

    public LoanService(
        IBookRepository bookRepository,
        IUserRepository userRepository,
        ILoanRepository loanRepository,
        IReservationRepository reservationRepository,
        ITransactionRepository transactionRepository,
        ILibraryUnitOfWork unitOfWork,
        LibrarySettings librarySettings)
    {
        _bookRepository = bookRepository;
        _userRepository = userRepository;
        _loanRepository = loanRepository;
        _reservationRepository = reservationRepository;
        _transactionRepository = transactionRepository;
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
        IssueInternalAsync(request.BookId, memberUserId, request.BorrowDays, memberUserId, cancellationToken);

    public async Task<OperationResult<LoanDto>> IssueAsync(IssueLoanRequest request, int issuedByUserId, CancellationToken cancellationToken = default)
    {
        return await IssueInternalAsync(request.BookId, request.MemberId, request.BorrowDays, issuedByUserId, cancellationToken);
    }

    private async Task<OperationResult<LoanDto>> IssueInternalAsync(
        int bookId,
        int memberId,
        int borrowDays,
        int issuedByUserId,
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

        var book = await _bookRepository.GetByIdAsync(bookId, cancellationToken);
        if (book is null || !book.IsActive)
        {
            return OperationResult<LoanDto>.Failure("Book not found", FailureType.NotFound);
        }

        if (book.AvailableCopies <= 0)
        {
            return OperationResult<LoanDto>.Failure("No copies are currently available", FailureType.Conflict);
        }

        var member = await _userRepository.GetByIdAsync(memberId, cancellationToken);
        if (member is null || member.Role != UserRole.Member)
        {
            return OperationResult<LoanDto>.Failure("Member account not found", FailureType.NotFound);
        }

        if (!member.IsActive)
        {
            return OperationResult<LoanDto>.Failure("Member account is inactive", FailureType.Conflict);
        }

        var activeLoans = await _loanRepository.GetLoansAsync(member.Id, activeOnly: true, cancellationToken);
        if (activeLoans.Count >= _librarySettings.MaxConcurrentLoans)
        {
            return OperationResult<LoanDto>.Failure($"A member can only borrow {_librarySettings.MaxConcurrentLoans} different books at a time", FailureType.Conflict);
        }

        var existingLoan = await _loanRepository.GetActiveLoanAsync(bookId, memberId, cancellationToken);
        if (existingLoan is not null)
        {
            return OperationResult<LoanDto>.Failure("This member already has an active loan for the selected book", FailureType.Conflict);
        }

        var queuedReservation = await _reservationRepository.GetNextActiveReservationAsync(bookId, cancellationToken);
        if (queuedReservation is not null && queuedReservation.MemberId != memberId)
        {
            return OperationResult<LoanDto>.Failure("This book is reserved for another member", FailureType.Conflict);
        }

        var issuedAt = DateTime.UtcNow;
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

        book.AvailableCopies -= 1;

        if (queuedReservation is not null && queuedReservation.MemberId == memberId)
        {
            queuedReservation.Status = ReservationStatus.Fulfilled;
            queuedReservation.FulfilledAt = issuedAt;
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
                Details = $"Book '{book.Title}' was issued to '{member.Username}'."
            },
            cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return OperationResult<LoanDto>.Success(Map(loan));
    }

    public async Task<OperationResult<LoanDto>> ReturnAsync(int loanId, int returnedByUserId, CancellationToken cancellationToken = default)
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

        var returnedAt = DateTime.UtcNow;
        loan.Status = LoanStatus.Returned;
        loan.ReturnedAt = returnedAt;
        loan.Book.AvailableCopies = Math.Min(loan.Book.TotalCopies, loan.Book.AvailableCopies + 1);

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

        var nextReservation = await _reservationRepository.GetNextActiveReservationAsync(loan.BookId, cancellationToken);
        if (nextReservation is not null && nextReservation.Status == ReservationStatus.Active)
        {
            nextReservation.Status = ReservationStatus.Available;
            nextReservation.NotifiedAt = returnedAt;

            await _transactionRepository.AddAsync(
                new TransactionRecord
                {
                    Type = TransactionType.ReservationAvailable,
                    BookId = nextReservation.BookId,
                    UserId = nextReservation.MemberId,
                    PerformedById = returnedByUserId,
                    ReservationId = nextReservation.Id,
                    Details = $"Reserved book '{loan.Book.Title}' is now available for member '{nextReservation.Member?.Username ?? nextReservation.MemberId.ToString()}'."
                },
                cancellationToken);
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);

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
            FineCalculator.CalculateOutstanding(loan, loan.FinePayments, _librarySettings.FinePerDay, DateTime.UtcNow),
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
}
