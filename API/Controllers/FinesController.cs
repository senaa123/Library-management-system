using LibraryM.Application.Fines;
using LibraryM.Application.Fines.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LibraryM.WebApi.Controllers;

[Authorize]
[Route("api/[controller]")]
[ApiController]
public class FinesController : ApiControllerBase
{
    private readonly IFineService _fineService;

    public FinesController(IFineService fineService)
    {
        _fineService = fineService;
    }

    [HttpGet("summary")]
    public async Task<IActionResult> GetSummary([FromQuery] int? memberId, CancellationToken cancellationToken)
    {
        var result = await _fineService.GetSummaryAsync(memberId, GetCurrentUserId(), GetCurrentUserRole(), cancellationToken);
        return result.IsSuccess && result.Value is not null ? Ok(result.Value) : ToFailureResult(result);
    }

    [HttpGet("payments")]
    public async Task<IActionResult> GetPayments([FromQuery] int? memberId, CancellationToken cancellationToken)
    {
        var result = await _fineService.GetPaymentsAsync(memberId, GetCurrentUserId(), GetCurrentUserRole(), cancellationToken);
        return result.IsSuccess && result.Value is not null ? Ok(result.Value) : ToFailureResult(result);
    }

    [Authorize(Policy = "StaffOnly")]
    [HttpPost("payments")]
    public async Task<IActionResult> RecordPayment([FromBody] FinePaymentRequest request, CancellationToken cancellationToken)
    {
        var result = await _fineService.RecordPaymentAsync(request, GetCurrentUserId(), cancellationToken);
        return result.IsSuccess && result.Value is not null ? Ok(result.Value) : ToFailureResult(result);
    }

    [Authorize(Roles = "Member")]
    [HttpPost("checkout/session")]
    public async Task<IActionResult> CreateCheckoutSession(CancellationToken cancellationToken)
    {
        var result = await _fineService.CreateCheckoutSessionAsync(GetCurrentUserId(), cancellationToken);
        return result.IsSuccess && result.Value is not null ? Ok(result.Value) : ToFailureResult(result);
    }

    [Authorize(Roles = "Member")]
    [HttpPost("checkout/complete")]
    public async Task<IActionResult> CompleteCheckout([FromBody] CompleteFineCheckoutRequest request, CancellationToken cancellationToken)
    {
        var result = await _fineService.CompleteCheckoutAsync(request.SessionId, GetCurrentUserId(), cancellationToken);
        return result.IsSuccess && result.Value is not null ? Ok(result.Value) : ToFailureResult(result);
    }
}
