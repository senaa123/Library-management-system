using LibraryM.Application.Abstractions.Persistence;
using LibraryM.Application.Common;
using LibraryM.Application.Configuration;
using LibraryM.Application.Fines.Models;
using LibraryM.Domain.Entities;
using LibraryM.Domain.Enums;

namespace LibraryM.Application.Fines;

public sealed class FineService : IFineService
{
    private readonly ILoanRepository _loanRepository;
    private readonly IUserRepository _userRepository;
    private readonly IFinePaymentRepository _finePaymentRepository;
    private readonly ITransactionRepository _transactionRepository;
    private readonly ILibraryUnitOfWork _unitOfWork;
    private readonly LibrarySettings _librarySettings;

    public FineService(
        ILoanRepository loanRepository,
        IUserRepository userRepository,
        IFinePaymentRepository finePaymentRepository,
        ITransactionRepository transactionRepository,
        ILibraryUnitOfWork unitOfWork,
        LibrarySettings librarySettings)
    {
        _loanRepository = loanRepository;
        _userRepository = userRepository;
        _finePaymentRepository = finePaymentRepository;
        _transactionRepository = transactionRepository;
        _unitOfWork = unitOfWork;
        _librarySettings = librarySettings;
    }

    public async Task<OperationResult<FineSummaryDto>> GetSummaryAsync(
        int? memberId,
        int requesterUserId,
        UserRole requesterRole,
        CancellationToken cancellationToken = default)
    {
        var effectiveMemberId = requesterRole == UserRole.Member ? requesterUserId : memberId;
        var loans = await _loanRepository.GetLoansAsync(effectiveMemberId, activeOnly: false, cancellationToken);
        var payments = await _finePaymentRepository.GetPaymentsByLoanIdsAsync(loans.Select(loan => loan.Id), cancellationToken);
        var paymentsByLoan = payments.GroupBy(payment => payment.LoanId).ToDictionary(group => group.Key, group => group.ToList());

        var items = loans
            .Select(loan =>
            {
                paymentsByLoan.TryGetValue(loan.Id, out var loanPayments);
                var accruedAmount = FineCalculator.CalculateAccruedFine(loan, _librarySettings.FinePerDay, DateTime.UtcNow);
                var paidAmount = loanPayments?.Sum(payment => payment.Amount) ?? 0m;
                var outstandingAmount = Math.Max(0m, accruedAmount - paidAmount);

                return new FineItemDto(
                    loan.Id,
                    loan.BookId,
                    loan.Book?.Title ?? string.Empty,
                    loan.DueDate,
                    loan.ReturnedAt,
                    accruedAmount,
                    paidAmount,
                    outstandingAmount);
            })
            .Where(item => item.AccruedAmount > 0m || item.PaidAmount > 0m)
            .OrderByDescending(item => item.OutstandingAmount)
            .ThenBy(item => item.DueDate)
            .ToList();

        var summary = new FineSummaryDto(
            items.Sum(item => item.AccruedAmount),
            items.Sum(item => item.PaidAmount),
            items.Sum(item => item.OutstandingAmount),
            items);

        return OperationResult<FineSummaryDto>.Success(summary);
    }

    public async Task<OperationResult<IReadOnlyList<FinePaymentRecordDto>>> GetPaymentsAsync(
        int? memberId,
        int requesterUserId,
        UserRole requesterRole,
        CancellationToken cancellationToken = default)
    {
        var effectiveMemberId = requesterRole == UserRole.Member ? requesterUserId : memberId;
        var payments = await _finePaymentRepository.GetPaymentsAsync(effectiveMemberId, cancellationToken);
        var paymentDtos = payments
            .OrderByDescending(payment => payment.PaidAt)
            .Select(Map)
            .ToList();

        return OperationResult<IReadOnlyList<FinePaymentRecordDto>>.Success(paymentDtos);
    }

    public async Task<OperationResult<FinePaymentRecordDto>> RecordPaymentAsync(
        FinePaymentRequest request,
        int receivedByUserId,
        CancellationToken cancellationToken = default)
    {
        if (request.LoanId <= 0 || request.Amount <= 0m)
        {
            return OperationResult<FinePaymentRecordDto>.Failure("Loan and payment amount are required", FailureType.Validation);
        }

        var loan = await _loanRepository.GetByIdAsync(request.LoanId, cancellationToken);
        if (loan is null)
        {
            return OperationResult<FinePaymentRecordDto>.Failure("Loan not found", FailureType.NotFound);
        }

        var receiver = await _userRepository.GetByIdAsync(receivedByUserId, cancellationToken);
        if (receiver is null)
        {
            return OperationResult<FinePaymentRecordDto>.Failure("Receiving staff account was not found", FailureType.NotFound);
        }

        var existingPayments = await _finePaymentRepository.GetPaymentsByLoanIdsAsync(new[] { loan.Id }, cancellationToken);
        var outstandingAmount = FineCalculator.CalculateOutstanding(loan, existingPayments, _librarySettings.FinePerDay, DateTime.UtcNow);

        if (outstandingAmount <= 0m)
        {
            return OperationResult<FinePaymentRecordDto>.Failure("This loan does not have an outstanding fine", FailureType.Conflict);
        }

        var roundedAmount = Math.Round(request.Amount, 2, MidpointRounding.AwayFromZero);
        if (roundedAmount > outstandingAmount)
        {
            return OperationResult<FinePaymentRecordDto>.Failure("Payment amount exceeds the outstanding fine", FailureType.Validation);
        }

        var payment = new FinePayment
        {
            LoanId = loan.Id,
            Loan = loan,
            MemberId = loan.BorrowerId,
            Member = loan.Borrower,
            ReceivedById = receiver.Id,
            ReceivedBy = receiver,
            Amount = roundedAmount,
            PaidAt = DateTime.UtcNow,
            Notes = request.Notes?.Trim() ?? string.Empty
        };

        await _finePaymentRepository.AddAsync(payment, cancellationToken);
        await _transactionRepository.AddAsync(
            new TransactionRecord
            {
                Type = TransactionType.FinePayment,
                BookId = loan.BookId,
                UserId = loan.BorrowerId,
                PerformedById = receivedByUserId,
                LoanId = loan.Id,
                FinePayment = payment,
                Details = $"Fine payment of {roundedAmount:0.00} recorded for loan #{loan.Id}."
            },
            cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return OperationResult<FinePaymentRecordDto>.Success(Map(payment));
    }

    private static FinePaymentRecordDto Map(FinePayment payment) =>
        new(
            payment.Id,
            payment.LoanId,
            payment.MemberId,
            payment.Member?.FullName ?? string.Empty,
            payment.Amount,
            payment.PaidAt,
            payment.Notes,
            payment.ReceivedById,
            payment.ReceivedBy?.FullName ?? string.Empty);
}
