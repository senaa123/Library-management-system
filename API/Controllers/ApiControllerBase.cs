using System.Security.Claims;
using LibraryM.Application.Common;
using LibraryM.Domain.Enums;
using Microsoft.AspNetCore.Mvc;

namespace LibraryM.WebApi.Controllers;

public abstract class ApiControllerBase : ControllerBase
{
    protected int GetCurrentUserId()
    {
        var claimValue = User.FindFirstValue("id") ?? User.FindFirstValue(ClaimTypes.NameIdentifier);
        return int.TryParse(claimValue, out var userId)
            ? userId
            : throw new InvalidOperationException("Authenticated user ID claim is missing.");
    }

    protected UserRole GetCurrentUserRole()
    {
        var roleValue = User.FindFirstValue(ClaimTypes.Role);
        return Enum.TryParse<UserRole>(roleValue, true, out var role)
            ? role
            : throw new InvalidOperationException("Authenticated user role claim is missing.");
    }

    protected IActionResult ToFailureResult(OperationResult result) =>
        result.FailureType switch
        {
            FailureType.Validation => BadRequest(new { message = result.Message }),
            FailureType.Unauthorized => Unauthorized(new { message = result.Message }),
            FailureType.Forbidden => StatusCode(StatusCodes.Status403Forbidden, new { message = result.Message }),
            FailureType.NotFound => NotFound(new { message = result.Message }),
            FailureType.Conflict => Conflict(new { message = result.Message }),
            _ => BadRequest(new { message = result.Message })
        };
}
