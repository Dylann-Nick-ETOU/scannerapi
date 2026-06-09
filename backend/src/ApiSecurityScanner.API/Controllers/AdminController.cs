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
    GetAdminScanByIdUseCase getAdminScanByIdUseCase,
    CompareAdminScansUseCase compareAdminScansUseCase,
    DeactivateUserUseCase deactivateUserUseCase,
    ReactivateUserUseCase reactivateUserUseCase) : ControllerBase
{
    [HttpGet("users")]
    public async Task<ActionResult<IReadOnlyList<AdminUserActivityDto>>> GetUsers(CancellationToken cancellationToken)
    {
        var users = await getAdminUserActivityUseCase.ExecuteAsync(cancellationToken);
        return Ok(users);
    }

    [HttpGet("scans/{id:guid}")]
    public async Task<ActionResult<ScanReportDto>> GetScanById(Guid id, CancellationToken cancellationToken)
    {
        var scan = await getAdminScanByIdUseCase.ExecuteAsync(id, cancellationToken);
        if (scan is null)
        {
            return NotFound(new { message = "Scan not found." });
        }

        return Ok(scan);
    }

    [HttpGet("scans/{currentScanId:guid}/compare/{baselineScanId:guid}")]
    public async Task<ActionResult<ScanComparisonDto>> CompareScans(
        Guid currentScanId,
        Guid baselineScanId,
        CancellationToken cancellationToken)
    {
        var comparison = await compareAdminScansUseCase.ExecuteAsync(currentScanId, baselineScanId, cancellationToken);
        if (comparison is null)
        {
            return NotFound(new { message = "One or both scans were not found." });
        }

        return Ok(comparison);
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

    [HttpPost("users/{username}/reactivate")]
    public async Task<IActionResult> ReactivateUser(string username, CancellationToken cancellationToken)
    {
        var reactivated = await reactivateUserUseCase.ExecuteAsync(username, cancellationToken);
        if (!reactivated)
        {
            return NotFound(new { message = "User not found." });
        }

        return NoContent();
    }
}
