using Microsoft.AspNetCore.Mvc;

namespace ApiSecurityScanner.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ScansController : ControllerBase
{
    [HttpPost("url")]
    public IActionResult ScanFromUrl() => StatusCode(StatusCodes.Status501NotImplemented);

    [HttpPost("file")]
    public IActionResult ScanFromFile() => StatusCode(StatusCodes.Status501NotImplemented);

    [HttpGet]
    public IActionResult GetAllScans() => StatusCode(StatusCodes.Status501NotImplemented);

    [HttpGet("{id:guid}")]
    public IActionResult GetScanById(Guid id) => StatusCode(StatusCodes.Status501NotImplemented);

    [HttpDelete("{id:guid}")]
    public IActionResult DeleteScan(Guid id) => StatusCode(StatusCodes.Status501NotImplemented);
}
