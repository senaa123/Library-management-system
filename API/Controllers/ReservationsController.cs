using LibraryM.Application.Reservations;
using LibraryM.Application.Reservations.Models;
using LibraryM.Application.Loans;
using LibraryM.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LibraryM.WebApi.Controllers;

[Authorize]
[Route("api/[controller]")]
[ApiController]
public class ReservationsController : ApiControllerBase
{
    private readonly IReservationService _reservationService;
    private readonly ILoanService _loanService;

    public ReservationsController(IReservationService reservationService, ILoanService loanService)
    {
        _reservationService = reservationService;
        _loanService = loanService;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<ReservationDto>>> GetReservations([FromQuery] int? memberId, [FromQuery] ReservationStatus? status, CancellationToken cancellationToken)
    {
        var reservations = await _reservationService.GetReservationsAsync(memberId, status, GetCurrentUserRole(), GetCurrentUserId(), cancellationToken);
        return Ok(reservations);
    }

    [Authorize(Roles = "Member")]
    [HttpPost]
    public async Task<IActionResult> CreateReservation([FromBody] CreateReservationRequest request, CancellationToken cancellationToken)
    {
        var result = await _reservationService.CreateAsync(request, GetCurrentUserId(), GetCurrentUserRole(), cancellationToken);
        if (!result.IsSuccess || result.Value is null)
        {
            return ToFailureResult(result);
        }

        return Ok(result.Value);
    }

    [HttpPost("{id:int}/cancel")]
    public async Task<IActionResult> CancelReservation(int id, CancellationToken cancellationToken)
    {
        var result = await _reservationService.CancelAsync(id, GetCurrentUserId(), GetCurrentUserRole(), cancellationToken);
        return result.IsSuccess && result.Value is not null ? Ok(result.Value) : ToFailureResult(result);
    }

    [Authorize(Policy = "StaffOnly")]
    [HttpPost("{id:int}/issue")]
    public async Task<IActionResult> IssueReservation(int id, [FromBody] IssueReservationRequest request, CancellationToken cancellationToken)
    {
        var result = await _loanService.IssueReservationAsync(id, request.BorrowDays, GetCurrentUserId(), cancellationToken);
        return result.IsSuccess && result.Value is not null ? Ok(result.Value) : ToFailureResult(result);
    }
}
