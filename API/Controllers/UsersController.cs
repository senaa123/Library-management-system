using LibraryM.Application.Users;
using LibraryM.Application.Users.Models;
using LibraryM.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LibraryM.WebApi.Controllers;

[Authorize]
[Route("api/[controller]")]
[ApiController]
public class UsersController : ApiControllerBase
{
    private readonly IUserService _userService;

    public UsersController(IUserService userService)
    {
        _userService = userService;
    }

    [HttpGet("me")]
    public async Task<IActionResult> GetCurrentUser(CancellationToken cancellationToken)
    {
        var result = await _userService.GetByIdAsync(GetCurrentUserId(), cancellationToken);
        return result.IsSuccess && result.Value is not null ? Ok(result.Value) : ToFailureResult(result);
    }

    [HttpPut("me")]
    public async Task<IActionResult> UpdateProfile([FromBody] UpdateUserRequest request, CancellationToken cancellationToken)
    {
        var result = await _userService.UpdateProfileAsync(GetCurrentUserId(), request, cancellationToken);
        return result.IsSuccess && result.Value is not null ? Ok(result.Value) : ToFailureResult(result);
    }

    [Authorize(Policy = "StaffOnly")]
    [HttpGet]
    public async Task<ActionResult<IEnumerable<UserDto>>> GetUsers([FromQuery] UserRole? role, [FromQuery] bool? isActive, CancellationToken cancellationToken)
    {
        var users = await _userService.GetUsersAsync(role, isActive, cancellationToken);
        return Ok(users);
    }

    [Authorize(Policy = "StaffOnly")]
    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetUserById(int id, CancellationToken cancellationToken)
    {
        var result = await _userService.GetByIdAsync(id, cancellationToken);
        return result.IsSuccess && result.Value is not null ? Ok(result.Value) : ToFailureResult(result);
    }

    [Authorize(Policy = "AdminOnly")]
    [HttpPost("staff")]
    public async Task<IActionResult> CreateStaff([FromBody] CreateStaffRequest request, CancellationToken cancellationToken)
    {
        var result = await _userService.CreateStaffAsync(request, GetCurrentUserId(), cancellationToken);
        if (!result.IsSuccess || result.Value is null)
        {
            return ToFailureResult(result);
        }

        return CreatedAtAction(nameof(GetUserById), new { id = result.Value.Id }, result.Value);
    }

    [Authorize(Policy = "AdminOnly")]
    [HttpPut("{id:int}")]
    public async Task<IActionResult> UpdateUser(int id, [FromBody] UpdateUserRequest request, CancellationToken cancellationToken)
    {
        var result = await _userService.UpdateUserAsync(id, request, GetCurrentUserId(), cancellationToken);
        return result.IsSuccess && result.Value is not null ? Ok(result.Value) : ToFailureResult(result);
    }

    [Authorize(Policy = "StaffOnly")]
    [HttpPut("{id:int}/restriction")]
    public async Task<IActionResult> SetRestriction(int id, [FromBody] UpdateMemberRestrictionRequest request, CancellationToken cancellationToken)
    {
        var result = await _userService.SetRestrictionAsync(id, request, GetCurrentUserId(), cancellationToken);
        return result.IsSuccess && result.Value is not null ? Ok(result.Value) : ToFailureResult(result);
    }
}
