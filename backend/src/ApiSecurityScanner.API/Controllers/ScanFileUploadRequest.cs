namespace ApiSecurityScanner.API.Controllers;

public class ScanFileUploadRequest
{
    public IFormFile? File { get; set; }
    public string? TargetName { get; set; }
}
