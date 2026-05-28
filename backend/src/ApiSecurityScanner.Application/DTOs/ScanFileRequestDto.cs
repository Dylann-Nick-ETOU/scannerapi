namespace ApiSecurityScanner.Application.DTOs;

public class ScanFileRequestDto
{
    public string? TargetName { get; set; }
    public string FileContent { get; set; } = string.Empty;
}
