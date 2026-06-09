using ApiSecurityScanner.Application.DTOs;
using ApiSecurityScanner.Application.UseCases;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace ApiSecurityScanner.API.Controllers;

[ApiController]
[Authorize(Roles = "Admin")]
[Route("api/[controller]")]
public class AdminController(
    GetAdminUserActivityUseCase getAdminUserActivityUseCase,
    GetAdminAuditLogsUseCase getAdminAuditLogsUseCase,
    GetAdminScanByIdUseCase getAdminScanByIdUseCase,
    CompareAdminScansUseCase compareAdminScansUseCase,
    RecordAdminAuditLogUseCase recordAdminAuditLogUseCase,
    DeactivateUserUseCase deactivateUserUseCase,
    ReactivateUserUseCase reactivateUserUseCase) : ControllerBase
{
    [HttpGet("users")]
    public async Task<ActionResult<IReadOnlyList<AdminUserActivityDto>>> GetUsers(CancellationToken cancellationToken)
    {
        var users = await getAdminUserActivityUseCase.ExecuteAsync(cancellationToken);
        return Ok(users);
    }

    [HttpGet("audit-logs")]
    public async Task<ActionResult<IReadOnlyList<AdminAuditLogDto>>> GetAuditLogs(CancellationToken cancellationToken)
    {
        var logs = await getAdminAuditLogsUseCase.ExecuteAsync(cancellationToken: cancellationToken);
        return Ok(logs);
    }

    [HttpGet("scans/{id:guid}")]
    public async Task<ActionResult<ScanReportDto>> GetScanById(Guid id, CancellationToken cancellationToken)
    {
        var scan = await getAdminScanByIdUseCase.ExecuteAsync(id, cancellationToken);
        if (scan is null)
        {
            return NotFound(new { message = "Scan not found." });
        }

        await recordAdminAuditLogUseCase.ExecuteAsync(
            GetCurrentAdminUsername(),
            "ViewUserScanReport",
            null,
            id,
            $"Consultation du rapport utilisateur {id}.",
            cancellationToken);

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

        await recordAdminAuditLogUseCase.ExecuteAsync(
            GetCurrentAdminUsername(),
            "CompareUserScans",
            null,
            currentScanId,
            $"Comparaison admin entre les scans {baselineScanId} et {currentScanId}.",
            cancellationToken);

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

        await recordAdminAuditLogUseCase.ExecuteAsync(
            GetCurrentAdminUsername(),
            "DeactivateUser",
            username,
            null,
            $"Désactivation du compte {username}.",
            cancellationToken);

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

        await recordAdminAuditLogUseCase.ExecuteAsync(
            GetCurrentAdminUsername(),
            "ReactivateUser",
            username,
            null,
            $"Réactivation du compte {username}.",
            cancellationToken);

        return NoContent();
    }

    private string GetCurrentAdminUsername()
    {
        var username = User.FindFirstValue(ClaimTypes.Name)
            ?? User.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? User.FindFirstValue("sub");

        if (string.IsNullOrWhiteSpace(username))
        {
            throw new UnauthorizedAccessException("Missing name claim.");
        }

        return username;
    }
}
