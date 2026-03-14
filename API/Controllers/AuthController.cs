using LibraryM.Application.Auth;
using LibraryM.Application.Auth.Models;
using LibraryM.Application.Common;
using Microsoft.AspNetCore.Mvc;

namespace LibraryM.WebApi.Controllers;

[Route("api/[controller]")]
[ApiController]
public class AuthController : ControllerBase
{
    private readonly IAuthService _authService;

    public AuthController(IAuthService authService)
    {
        _authService = authService;
    }

    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] RegisterRequest request, CancellationToken cancellationToken)
    {
        var result = await _authService.RegisterAsync(request, cancellationToken);
        if (result.IsSuccess)
        {
            return Ok(new { message = result.Message });
        }

        return BadRequest(new { message = result.Message });
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequest request, CancellationToken cancellationToken)
    {
        var result = await _authService.LoginAsync(request, cancellationToken);
        if (result.IsSuccess && result.Value is not null)
        {
            return Ok(new
            {
                token = result.Value.Token,
                username = result.Value.Username,
                role = result.Value.Role
            });
        }

        return result.FailureType == FailureType.Validation
            ? BadRequest(new { message = result.Message })
            : Unauthorized(new { message = result.Message });
    }
}
