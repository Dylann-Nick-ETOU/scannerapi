using System.Text;
using System.Text.Json;
using System.Security.Claims;
using ApiSecurityScanner.Application.DTOs;
using ApiSecurityScanner.Application.UseCases;
using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace ApiSecurityScanner.API.Controllers;

[Authorize]
[EnableRateLimiting("ScanRequests")]
[ApiController]
[Route("api/[controller]")]
public class ScansController(
    ScanOpenApiUseCase scanOpenApiUseCase,
    ScanOpenApiFileUseCase scanOpenApiFileUseCase,
    GetAllScansUseCase getAllScansUseCase,
    GetScanByIdUseCase getScanByIdUseCase,
    DeleteScanUseCase deleteScanUseCase,
    IValidator<ScanRequestDto> validator) : ControllerBase
{
    private const long MaxUploadBytes = 2 * 1024 * 1024; // 2MB

    [HttpPost("url")]
    public async Task<ActionResult<ScanReportDto>> ScanFromUrl([FromBody] ScanRequestDto request, CancellationToken cancellationToken)
    {
        var ownerId = GetCurrentOwnerId();
        var validation = await validator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid)
        {
            return BadRequest(new
            {
                message = "Validation failed",
                errors = validation.Errors.Select(x => x.ErrorMessage)
            });
        }

        var report = await scanOpenApiUseCase.ExecuteAsync(request, ownerId, cancellationToken);
        return Ok(report);
    }

    [HttpPost("file")]
    [Consumes("multipart/form-data")]
    public async Task<ActionResult<ScanReportDto>> ScanFromFile([FromForm] ScanFileUploadRequest request, CancellationToken cancellationToken)
    {
        var ownerId = GetCurrentOwnerId();
        if (request.File is null || request.File.Length == 0)
        {
            return BadRequest(new { message = "File is required." });
        }

        if (request.File.Length > MaxUploadBytes)
        {
            return BadRequest(new { message = "File is too large. Maximum size is 2MB." });
        }

        var extension = Path.GetExtension(request.File.FileName).ToLowerInvariant();
        if (extension is not ".json" and not ".yaml" and not ".yml")
        {
            return BadRequest(new { message = "Only .json, .yaml, .yml files are accepted." });
        }

        await using var stream = request.File.OpenReadStream();
        using var reader = new StreamReader(stream);
        var content = await reader.ReadToEndAsync(cancellationToken);

        var report = await scanOpenApiFileUseCase.ExecuteAsync(new ScanFileRequestDto
        {
            TargetName = string.IsNullOrWhiteSpace(request.TargetName)
                ? Path.GetFileNameWithoutExtension(request.File.FileName)
                : request.TargetName,
            FileContent = content
        }, ownerId, cancellationToken);

        return Ok(report);
    }

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<ScanHistoryItemDto>>> GetAllScans(CancellationToken cancellationToken)
    {
        var scans = await getAllScansUseCase.ExecuteAsync(GetCurrentOwnerId(), cancellationToken);
        return Ok(scans);
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<ScanReportDto>> GetScanById(Guid id, CancellationToken cancellationToken)
    {
        var report = await getScanByIdUseCase.ExecuteAsync(id, GetCurrentOwnerId(), cancellationToken);
        if (report is null)
        {
            return NotFound(new { message = "Scan not found." });
        }

        return Ok(report);
    }

    [HttpGet("{id:guid}/export")]
    public async Task<IActionResult> ExportScan(Guid id, CancellationToken cancellationToken)
    {
        var report = await getScanByIdUseCase.ExecuteAsync(id, GetCurrentOwnerId(), cancellationToken);
        if (report is null)
        {
            return NotFound(new { message = "Scan not found." });
        }

        var payload = JsonSerializer.Serialize(report, new JsonSerializerOptions { WriteIndented = true });
        var bytes = Encoding.UTF8.GetBytes(payload);
        return File(bytes, "application/json", $"scan-{id}.json");
    }

    [HttpDelete("{id:guid}")]
    [Authorize(Policy = "ScanDelete")]
    public async Task<IActionResult> DeleteScan(Guid id, CancellationToken cancellationToken)
    {
        var deleted = await deleteScanUseCase.ExecuteAsync(id, GetCurrentOwnerId(), cancellationToken);
        if (!deleted)
        {
            return NotFound(new { message = "Scan not found." });
        }

        return NoContent();
    }

    private string GetCurrentOwnerId()
    {
        var ownerId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub");
        if (string.IsNullOrWhiteSpace(ownerId))
        {
            throw new UnauthorizedAccessException("Missing subject claim.");
        }

        return ownerId;
    }
}
