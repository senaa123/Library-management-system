using LibraryM.Application.Loans;
using LibraryM.Application.Loans.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LibraryM.WebApi.Controllers;

[Authorize]
[Route("api/[controller]")]
[ApiController]
public class LoansController : ApiControllerBase
{
    private readonly ILoanService _loanService;

    public LoansController(ILoanService loanService)
    {
        _loanService = loanService;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<LoanDto>>> GetLoans([FromQuery] int? memberId, [FromQuery] bool activeOnly = false, CancellationToken cancellationToken = default)
    {
        var loans = await _loanService.GetLoansAsync(memberId, activeOnly, GetCurrentUserRole(), GetCurrentUserId(), cancellationToken);
        return Ok(loans);
    }

    [Authorize(Policy = "StaffOnly")]
    [HttpPost("issue")]
    public async Task<IActionResult> IssueLoan([FromBody] IssueLoanRequest request, CancellationToken cancellationToken)
    {
        var result = await _loanService.IssueAsync(request, GetCurrentUserId(), cancellationToken);
        if (!result.IsSuccess || result.Value is null)
        {
            return ToFailureResult(result);
        }

        return Ok(result.Value);
    }

    [Authorize(Policy = "StaffOnly")]
    [HttpPost("issue-by-qr")]
    public async Task<IActionResult> IssueLoanByQr([FromBody] IssueLoanByQrRequest request, CancellationToken cancellationToken)
    {
        var result = await _loanService.IssueByQrAsync(request, GetCurrentUserId(), cancellationToken);
        return result.IsSuccess && result.Value is not null ? Ok(result.Value) : ToFailureResult(result);
    }

    [Authorize(Roles = "Member")]
    [HttpPost("borrow")]
    public async Task<IActionResult> BorrowBook([FromBody] BorrowBookRequest request, CancellationToken cancellationToken)
    {
        var result = await _loanService.BorrowAsync(request, GetCurrentUserId(), cancellationToken);
        return result.IsSuccess && result.Value is not null ? Ok(result.Value) : ToFailureResult(result);
    }

    [Authorize(Policy = "StaffOnly")]
    [HttpPost("{id:int}/return")]
    public async Task<IActionResult> ReturnLoan(int id, CancellationToken cancellationToken)
    {
        var result = await _loanService.ReturnAsync(id, GetCurrentUserId(), cancellationToken);
        return result.IsSuccess && result.Value is not null ? Ok(result.Value) : ToFailureResult(result);
    }

    [HttpPost("{id:int}/renew")]
    public async Task<IActionResult> RenewLoan(int id, CancellationToken cancellationToken)
    {
        var result = await _loanService.RenewAsync(id, GetCurrentUserId(), GetCurrentUserRole(), cancellationToken);
        return result.IsSuccess && result.Value is not null ? Ok(result.Value) : ToFailureResult(result);
    }
}
