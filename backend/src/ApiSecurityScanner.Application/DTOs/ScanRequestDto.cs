using System.ComponentModel.DataAnnotations;

namespace ApiSecurityScanner.Application.DTOs;

public class ScanRequestDto
{
    [StringLength(120, MinimumLength = 2)]
    public string? TargetName { get; set; }

    [Required]
    [StringLength(2048, MinimumLength = 10)]
    [Url]
    public string OpenApiUrl { get; set; } = string.Empty;
}
