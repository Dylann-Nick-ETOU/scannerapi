namespace ApiSecurityScanner.Application.DTOs;

public class ScanRequestDto
{
    public string? TargetName { get; set; }
    public string? OpenApiUrl { get; set; }
}
