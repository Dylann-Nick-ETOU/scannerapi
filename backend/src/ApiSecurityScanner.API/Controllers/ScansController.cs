using ApiSecurityScanner.Application.DTOs;
using ApiSecurityScanner.Application.UseCases;
using FluentValidation;
using Microsoft.AspNetCore.Mvc;

namespace ApiSecurityScanner.API.Controllers;

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
    [HttpPost("url")]
    public async Task<ActionResult<ScanReportDto>> ScanFromUrl([FromBody] ScanRequestDto request, CancellationToken cancellationToken)
    {
        var validation = await validator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid)
        {
            return BadRequest(new
            {
                message = "Validation failed",
                errors = validation.Errors.Select(x => x.ErrorMessage)
            });
        }

        var report = await scanOpenApiUseCase.ExecuteAsync(request, cancellationToken);
        return Ok(report);
    }

    [HttpPost("file")]
    [Consumes("multipart/form-data")]
    public async Task<ActionResult<ScanReportDto>> ScanFromFile([FromForm] IFormFile file, [FromForm] string? targetName, CancellationToken cancellationToken)
    {
        if (file is null || file.Length == 0)
        {
            return BadRequest(new { message = "File is required." });
        }

        var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
        if (extension is not ".json" and not ".yaml" and not ".yml")
        {
            return BadRequest(new { message = "Only .json, .yaml, .yml files are accepted." });
        }

        await using var stream = file.OpenReadStream();
        using var reader = new StreamReader(stream);
        var content = await reader.ReadToEndAsync(cancellationToken);

        var report = await scanOpenApiFileUseCase.ExecuteAsync(new ScanFileRequestDto
        {
            TargetName = string.IsNullOrWhiteSpace(targetName) ? Path.GetFileNameWithoutExtension(file.FileName) : targetName,
            FileContent = content
        }, cancellationToken);

        return Ok(report);
    }

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<ScanHistoryItemDto>>> GetAllScans(CancellationToken cancellationToken)
    {
        var scans = await getAllScansUseCase.ExecuteAsync(cancellationToken);
        return Ok(scans);
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<ScanReportDto>> GetScanById(Guid id, CancellationToken cancellationToken)
    {
        var report = await getScanByIdUseCase.ExecuteAsync(id, cancellationToken);
        if (report is null)
        {
            return NotFound(new { message = "Scan not found." });
        }

        return Ok(report);
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> DeleteScan(Guid id, CancellationToken cancellationToken)
    {
        var deleted = await deleteScanUseCase.ExecuteAsync(id, cancellationToken);
        if (!deleted)
        {
            return NotFound(new { message = "Scan not found." });
        }

        return NoContent();
    }
}
