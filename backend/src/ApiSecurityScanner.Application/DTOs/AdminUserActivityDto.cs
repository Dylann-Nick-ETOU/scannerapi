namespace ApiSecurityScanner.Application.DTOs;

public class AdminUserActivityDto
{
    public string Username { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? LastLoginAt { get; set; }
    public int ScansCount { get; set; }
    public DateTime? LastScanAt { get; set; }
    public List<AdminUserScanItemDto> Scans { get; set; } = new();
}
