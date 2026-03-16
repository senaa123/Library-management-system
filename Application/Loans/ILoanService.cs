using LibraryM.Application.Common;
using LibraryM.Application.Loans.Models;
using LibraryM.Domain.Enums;

namespace LibraryM.Application.Loans;

public interface ILoanService
{
    Task<IReadOnlyList<LoanDto>> GetLoansAsync(int? memberId, bool activeOnly, UserRole requesterRole, int requesterUserId, CancellationToken cancellationToken = default);

    Task<OperationResult<LoanDto>> IssueAsync(IssueLoanRequest request, int issuedByUserId, CancellationToken cancellationToken = default);

    Task<OperationResult<LoanDto>> IssueByQrAsync(IssueLoanByQrRequest request, int issuedByUserId, CancellationToken cancellationToken = default);

    Task<OperationResult<LoanDto>> IssueReservationAsync(int reservationId, int borrowDays, int issuedByUserId, CancellationToken cancellationToken = default);

    Task<OperationResult<LoanDto>> BorrowAsync(BorrowBookRequest request, int memberUserId, CancellationToken cancellationToken = default);

    Task<OperationResult<LoanDto>> ReturnAsync(int loanId, int returnedByUserId, CancellationToken cancellationToken = default);

    Task<OperationResult<LoanDto>> RenewAsync(int loanId, int requesterUserId, UserRole requesterRole, CancellationToken cancellationToken = default);
}
