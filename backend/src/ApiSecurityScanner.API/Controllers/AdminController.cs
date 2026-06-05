using ApiSecurityScanner.Application.DTOs;
using ApiSecurityScanner.Application.UseCases;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ApiSecurityScanner.API.Controllers;

[ApiController]
[Authorize(Roles = "Admin")]
[Route("api/[controller]")]
public class AdminController(
    GetAdminUserActivityUseCase getAdminUserActivityUseCase,
    DeactivateUserUseCase deactivateUserUseCase) : ControllerBase
{
    [HttpGet("users")]
    public async Task<ActionResult<IReadOnlyList<AdminUserActivityDto>>> GetUsers(CancellationToken cancellationToken)
    {
        var users = await getAdminUserActivityUseCase.ExecuteAsync(cancellationToken);
        return Ok(users);
    }

    [HttpPost("users/{username}/deactivate")]
    public async Task<IActionResult> DeactivateUser(string username, CancellationToken cancellationToken)
    {
        var deactivated = await deactivateUserUseCase.ExecuteAsync(username, cancellationToken);
        if (!deactivated)
        {
            return NotFound(new { message = "User not found." });
        }

        return NoContent();
    }
}
