using LibraryM.Application.Abstractions.Payments;
using LibraryM.Application.Abstractions.Persistence;
using LibraryM.Application.Common;
using LibraryM.Application.Configuration;
using LibraryM.Application.Fines.Models;
using LibraryM.Domain.Entities;
using LibraryM.Domain.Enums;

namespace LibraryM.Application.Fines;

public sealed class FineService : IFineService
{
    private sealed record LedgerItem(
        int? LoanId,
        int? ReservationId,
        int? BookId,
        string BookTitle,
        string FineType,
        string Description,
        DateTime AssessedAt,
        DateTime DueDate,
        DateTime? ReturnedAt,
        decimal AccruedAmount);

    private sealed record LedgerSnapshot(
        User Member,
        decimal TotalAccrued,
        decimal TotalPaid,
        decimal TotalOutstanding,
        IReadOnlyList<FineItemDto> Items,
        IReadOnlyList<FinePayment> Payments);

    private readonly ILoanRepository _loanRepository;
    private readonly IUserRepository _userRepository;
    private readonly IFineChargeRepository _fineChargeRepository;
    private readonly IFinePaymentRepository _finePaymentRepository;
    private readonly ITransactionRepository _transactionRepository;
    private readonly IFineCheckoutGateway _fineCheckoutGateway;
    private readonly ILibraryUnitOfWork _unitOfWork;
    private readonly LibrarySettings _librarySettings;

    public FineService(
        ILoanRepository loanRepository,
        IUserRepository userRepository,
        IFineChargeRepository fineChargeRepository,
        IFinePaymentRepository finePaymentRepository,
        ITransactionRepository transactionRepository,
        IFineCheckoutGateway fineCheckoutGateway,
        ILibraryUnitOfWork unitOfWork,
        LibrarySettings librarySettings)
    {
        _loanRepository = loanRepository;
        _userRepository = userRepository;
        _fineChargeRepository = fineChargeRepository;
        _finePaymentRepository = finePaymentRepository;
        _transactionRepository = transactionRepository;
        _fineCheckoutGateway = fineCheckoutGateway;
        _unitOfWork = unitOfWork;
        _librarySettings = librarySettings;
    }

    public async Task<OperationResult<FineSummaryDto>> GetSummaryAsync(
        int? memberId,
        int requesterUserId,
        UserRole requesterRole,
        CancellationToken cancellationToken = default)
    {
        var effectiveMemberId = ResolveMemberId(memberId, requesterUserId, requesterRole);
        if (effectiveMemberId <= 0)
        {
            return OperationResult<FineSummaryDto>.Failure("Member is required", FailureType.Validation);
        }

        var snapshot = await BuildLedgerSnapshotAsync(effectiveMemberId, cancellationToken);
        if (snapshot is null)
        {
            return OperationResult<FineSummaryDto>.Failure("Member account not found", FailureType.NotFound);
        }

        var summary = new FineSummaryDto(
            snapshot.TotalAccrued,
            snapshot.TotalPaid,
            snapshot.TotalOutstanding,
            BuildMemberStatus(snapshot.Member, snapshot.TotalOutstanding),
            snapshot.Items);

        return OperationResult<FineSummaryDto>.Success(summary);
    }

    public async Task<OperationResult<IReadOnlyList<FinePaymentRecordDto>>> GetPaymentsAsync(
        int? memberId,
        int requesterUserId,
        UserRole requesterRole,
        CancellationToken cancellationToken = default)
    {
        var effectiveMemberId = ResolveMemberId(memberId, requesterUserId, requesterRole);
        if (effectiveMemberId <= 0)
        {
            return OperationResult<IReadOnlyList<FinePaymentRecordDto>>.Failure("Member is required", FailureType.Validation);
        }

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
        if (request.MemberId <= 0 || request.Amount <= 0m)
        {
            return OperationResult<FinePaymentRecordDto>.Failure("Member and payment amount are required", FailureType.Validation);
        }

        var member = await _userRepository.GetByIdAsync(request.MemberId, cancellationToken);
        if (member is null || member.Role != UserRole.Member)
        {
            return OperationResult<FinePaymentRecordDto>.Failure("Member account not found", FailureType.NotFound);
        }

        var receiver = await _userRepository.GetByIdAsync(receivedByUserId, cancellationToken);
        if (receiver is null)
        {
            return OperationResult<FinePaymentRecordDto>.Failure("Receiving staff account was not found", FailureType.NotFound);
        }

        Loan? loan = null;
        if (request.LoanId.HasValue)
        {
            loan = await _loanRepository.GetByIdAsync(request.LoanId.Value, cancellationToken);
            if (loan is null || loan.BorrowerId != member.Id)
            {
                return OperationResult<FinePaymentRecordDto>.Failure("Loan not found for this member", FailureType.NotFound);
            }
        }

        var snapshot = await BuildLedgerSnapshotAsync(member.Id, cancellationToken);
        if (snapshot is null || snapshot.TotalOutstanding <= 0m)
        {
            return OperationResult<FinePaymentRecordDto>.Failure("This member does not have an outstanding fine", FailureType.Conflict);
        }

        var roundedAmount = Math.Round(request.Amount, 2, MidpointRounding.AwayFromZero);
        if (roundedAmount > snapshot.TotalOutstanding)
        {
            return OperationResult<FinePaymentRecordDto>.Failure("Payment amount exceeds the outstanding fine", FailureType.Validation);
        }

        var payment = new FinePayment
        {
            LoanId = request.LoanId,
            Loan = loan,
            MemberId = member.Id,
            Member = member,
            ReceivedById = receiver.Id,
            ReceivedBy = receiver,
            Amount = roundedAmount,
            PaidAt = DateTime.UtcNow,
            Notes = request.Notes?.Trim() ?? string.Empty,
            PaymentMethod = "Desk payment",
            ExternalReference = $"desk-{Guid.NewGuid():N}"
        };

        await _finePaymentRepository.AddAsync(payment, cancellationToken);
        await _transactionRepository.AddAsync(
            new TransactionRecord
            {
                Type = TransactionType.FinePayment,
                BookId = loan?.BookId,
                UserId = member.Id,
                PerformedById = receiver.Id,
                LoanId = loan?.Id,
                FinePayment = payment,
                Details = $"Desk fine payment of ${roundedAmount:0.00} recorded for '{member.Username}'."
            },
            cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return OperationResult<FinePaymentRecordDto>.Success(Map(payment));
    }

    public async Task<MemberFineStatusDto> GetMemberStatusAsync(int memberId, CancellationToken cancellationToken = default)
    {
        var snapshot = await BuildLedgerSnapshotAsync(memberId, cancellationToken);
        return snapshot is null
            ? new MemberFineStatusDto(0m, _librarySettings.MaxConcurrentLoans, false, false, false, null, string.Empty, string.Empty)
            : BuildMemberStatus(snapshot.Member, snapshot.TotalOutstanding);
    }

    public async Task<OperationResult> AddChargeAsync(CreateFineChargeRequest request, CancellationToken cancellationToken = default)
    {
        if (request.MemberId <= 0 || request.Amount <= 0m)
        {
            return OperationResult.Failure("Member and charge amount are required", FailureType.Validation);
        }

        var member = await _userRepository.GetByIdAsync(request.MemberId, cancellationToken);
        if (member is null || member.Role != UserRole.Member)
        {
            return OperationResult.Failure("Member account not found", FailureType.NotFound);
        }

        var externalReference = string.IsNullOrWhiteSpace(request.ExternalReference)
            ? $"charge-{Guid.NewGuid():N}"
            : request.ExternalReference.Trim();

        if (await _fineChargeRepository.ExistsByExternalReferenceAsync(externalReference, cancellationToken))
        {
            return OperationResult.Success("Charge already exists.");
        }

        var charge = new FineCharge
        {
            MemberId = member.Id,
            Member = member,
            LoanId = request.LoanId,
            ReservationId = request.ReservationId,
            CreatedById = request.CreatedByUserId,
            ChargeType = request.ChargeType,
            Amount = Math.Round(request.Amount, 2, MidpointRounding.AwayFromZero),
            Description = request.Description.Trim(),
            ExternalReference = externalReference,
            CreatedAt = DateTime.UtcNow
        };

        await _fineChargeRepository.AddAsync(charge, cancellationToken);
        await _transactionRepository.AddAsync(
            new TransactionRecord
            {
                Type = TransactionType.FineChargeAdded,
                BookId = charge.Loan?.BookId ?? charge.Reservation?.BookId,
                UserId = member.Id,
                PerformedById = request.CreatedByUserId,
                LoanId = charge.LoanId,
                ReservationId = charge.ReservationId,
                Details = $"{MapChargeTypeLabel(request.ChargeType)} fine of ${charge.Amount:0.00} added for '{member.Username}'."
            },
            cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return OperationResult.Success();
    }

    public async Task<OperationResult<CreateFineCheckoutSessionResult>> CreateCheckoutSessionAsync(int memberId, CancellationToken cancellationToken = default)
    {
        var snapshot = await BuildLedgerSnapshotAsync(memberId, cancellationToken);
        if (snapshot is null)
        {
            return OperationResult<CreateFineCheckoutSessionResult>.Failure("Member account not found", FailureType.NotFound);
        }

        if (snapshot.TotalOutstanding <= 0m)
        {
            return OperationResult<CreateFineCheckoutSessionResult>.Failure("There is no outstanding fine to pay.", FailureType.Conflict);
        }

        return await _fineCheckoutGateway.CreateSessionAsync(
            snapshot.Member.Id,
            snapshot.Member.FullName,
            snapshot.TotalOutstanding,
            cancellationToken);
    }

    public async Task<OperationResult<FinePaymentRecordDto>> CompleteCheckoutAsync(
        string sessionId,
        int memberId,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(sessionId))
        {
            return OperationResult<FinePaymentRecordDto>.Failure("Stripe session id is required.", FailureType.Validation);
        }

        var existingPayment = await _finePaymentRepository.GetByExternalReferenceAsync(sessionId.Trim(), cancellationToken);
        if (existingPayment is not null)
        {
            return OperationResult<FinePaymentRecordDto>.Success(Map(existingPayment));
        }

        var member = await _userRepository.GetByIdAsync(memberId, cancellationToken);
        if (member is null || member.Role != UserRole.Member)
        {
            return OperationResult<FinePaymentRecordDto>.Failure("Member account not found", FailureType.NotFound);
        }

        var verifiedPayment = await _fineCheckoutGateway.VerifyCompletedSessionAsync(sessionId.Trim(), cancellationToken);
        if (!verifiedPayment.IsSuccess || verifiedPayment.Value is null)
        {
            return OperationResult<FinePaymentRecordDto>.Failure(verifiedPayment.Message ?? "Stripe verification failed.", verifiedPayment.FailureType ?? FailureType.Conflict);
        }

        if (!verifiedPayment.Value.IsPaid)
        {
            return OperationResult<FinePaymentRecordDto>.Failure("Stripe has not marked this checkout session as paid yet.", FailureType.Conflict);
        }

        var snapshot = await BuildLedgerSnapshotAsync(member.Id, cancellationToken);
        if (snapshot is null || snapshot.TotalOutstanding <= 0m)
        {
            return OperationResult<FinePaymentRecordDto>.Failure("There is no outstanding fine to settle.", FailureType.Conflict);
        }

        var amountToRecord = Math.Min(snapshot.TotalOutstanding, verifiedPayment.Value.AmountPaid);
        if (amountToRecord <= 0m)
        {
            return OperationResult<FinePaymentRecordDto>.Failure("Stripe reported a zero-value payment.", FailureType.Conflict);
        }

        var payment = new FinePayment
        {
            MemberId = member.Id,
            Member = member,
            Amount = Math.Round(amountToRecord, 2, MidpointRounding.AwayFromZero),
            PaidAt = DateTime.UtcNow,
            Notes = "Paid through Stripe checkout.",
            PaymentMethod = verifiedPayment.Value.PaymentMethod,
            ExternalReference = verifiedPayment.Value.ExternalReference
        };

        await _finePaymentRepository.AddAsync(payment, cancellationToken);
        await _transactionRepository.AddAsync(
            new TransactionRecord
            {
                Type = TransactionType.FinePayment,
                UserId = member.Id,
                FinePayment = payment,
                Details = $"Stripe fine payment of ${payment.Amount:0.00} recorded for '{member.Username}'."
            },
            cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return OperationResult<FinePaymentRecordDto>.Success(Map(payment));
    }

    private async Task<LedgerSnapshot?> BuildLedgerSnapshotAsync(int memberId, CancellationToken cancellationToken)
    {
        var member = await _userRepository.GetByIdAsync(memberId, cancellationToken);
        if (member is null || member.Role != UserRole.Member)
        {
            return null;
        }

        var loans = await _loanRepository.GetLoansAsync(member.Id, activeOnly: false, cancellationToken);
        var charges = await _fineChargeRepository.GetChargesAsync(member.Id, cancellationToken);
        var payments = await _finePaymentRepository.GetPaymentsAsync(member.Id, cancellationToken);
        var now = DateTime.UtcNow;
        var items = new List<LedgerItem>();

        foreach (var loan in loans)
        {
            var accruedAmount = FineCalculator.CalculateAccruedFine(loan, _librarySettings, now);
            if (accruedAmount <= 0m)
            {
                continue;
            }

            items.Add(
                new LedgerItem(
                    loan.Id,
                    null,
                    loan.BookId,
                    loan.Book?.Title ?? string.Empty,
                    "Late return",
                    BuildLateFineDescription(loan, now),
                    loan.DueDate,
                    loan.DueDate,
                    loan.ReturnedAt,
                    accruedAmount));
        }

        foreach (var charge in charges)
        {
            items.Add(
                new LedgerItem(
                    charge.LoanId,
                    charge.ReservationId,
                    charge.Loan?.BookId ?? charge.Reservation?.BookId,
                    charge.Loan?.Book?.Title ?? charge.Reservation?.Book?.Title ?? "Member account",
                    MapChargeTypeLabel(charge.ChargeType),
                    charge.Description,
                    charge.CreatedAt,
                    charge.CreatedAt,
                    charge.Loan?.ReturnedAt,
                    charge.Amount));
        }

        var orderedItems = items
            .OrderBy(item => item.AssessedAt)
            .ThenBy(item => item.BookTitle)
            .ToList();

        var totalPaid = payments.Sum(payment => payment.Amount);
        var remainingPayments = totalPaid;
        var fineItems = new List<FineItemDto>(orderedItems.Count);

        foreach (var item in orderedItems)
        {
            var paidAmount = Math.Min(item.AccruedAmount, remainingPayments);
            remainingPayments = Math.Max(0m, remainingPayments - paidAmount);
            var outstandingAmount = Math.Max(0m, item.AccruedAmount - paidAmount);

            fineItems.Add(
                new FineItemDto(
                    item.LoanId,
                    item.ReservationId,
                    item.BookId,
                    item.BookTitle,
                    item.FineType,
                    item.Description,
                    item.AssessedAt,
                    item.DueDate,
                    item.ReturnedAt,
                    item.AccruedAmount,
                    paidAmount,
                    outstandingAmount));
        }

        return new LedgerSnapshot(
            member,
            orderedItems.Sum(item => item.AccruedAmount),
            totalPaid,
            fineItems.Sum(item => item.OutstandingAmount),
            fineItems,
            payments);
    }

    private MemberFineStatusDto BuildMemberStatus(User member, decimal totalOutstanding)
    {
        var hasTemporaryRestriction = member.RestrictedUntilUtc.HasValue && member.RestrictedUntilUtc.Value > DateTime.UtcNow;
        var isFineLimited = totalOutstanding >= _librarySettings.LimitedCirculationFineThreshold && totalOutstanding < _librarySettings.BlockingFineThreshold;
        var isBlockedByFine = totalOutstanding >= _librarySettings.BlockingFineThreshold;
        var isBlocked = hasTemporaryRestriction || isBlockedByFine;
        var maxCirculationItems = isBlocked
            ? 0
            : isFineLimited
                ? _librarySettings.LimitedCirculationItems
                : _librarySettings.MaxConcurrentLoans;

        string warningMessage;
        if (hasTemporaryRestriction)
        {
            warningMessage = $"{member.RestrictionReason} Restricted until {member.RestrictedUntilUtc:yyyy-MM-dd}.";
        }
        else if (isBlockedByFine)
        {
            warningMessage = $"Outstanding fines of ${totalOutstanding:0.00} must be paid before new reservations or issues are allowed.";
        }
        else if (isFineLimited)
        {
            warningMessage = $"Outstanding fines of ${totalOutstanding:0.00} limit this member to {_librarySettings.LimitedCirculationItems} active reserved/borrowed books.";
        }
        else
        {
            warningMessage = string.Empty;
        }

        return new MemberFineStatusDto(
            totalOutstanding,
            maxCirculationItems,
            isFineLimited,
            isBlocked,
            hasTemporaryRestriction,
            member.RestrictedUntilUtc,
            member.RestrictionReason,
            warningMessage);
    }

    private static int ResolveMemberId(int? memberId, int requesterUserId, UserRole requesterRole) =>
        requesterRole == UserRole.Member ? requesterUserId : memberId.GetValueOrDefault();

    private static string BuildLateFineDescription(Loan loan, DateTime asOfUtc)
    {
        var endDate = (loan.ReturnedAt ?? asOfUtc).Date;
        var overdueDays = Math.Max(0, (endDate - loan.DueDate.Date).Days);
        return overdueDays == 1 ? "1 day overdue." : $"{overdueDays} days overdue.";
    }

    private static string MapChargeTypeLabel(FineChargeType chargeType) =>
        chargeType switch
        {
            FineChargeType.ReservationNoShow => "Reservation no-show",
            FineChargeType.DamagedBook => "Damaged book",
            FineChargeType.LostBook => "Lost book",
            FineChargeType.MissingPages => "Missing pages",
            _ => "Fine charge"
        };

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
            payment.ReceivedBy?.FullName ?? string.Empty,
            payment.PaymentMethod,
            payment.ExternalReference);
}
