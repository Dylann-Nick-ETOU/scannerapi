using ApiSecurityScanner.Application.DTOs;
using ApiSecurityScanner.Domain.Interfaces;

namespace ApiSecurityScanner.Application.UseCases;

public class GetAdminUserActivityUseCase(
    IUserRepository userRepository,
    IScanRepository scanRepository)
{
    public async Task<IReadOnlyList<AdminUserActivityDto>> ExecuteAsync(CancellationToken cancellationToken = default)
    {
        var users = await userRepository.GetAllAsync(cancellationToken);
        var scans = await scanRepository.GetAllWithIssuesAsync(cancellationToken);

        return users
            .Select(user =>
            {
                var userScans = scans
                    .Where(scan => string.Equals(scan.OwnerId, user.Username, StringComparison.OrdinalIgnoreCase))
                    .OrderByDescending(scan => scan.CreatedAt)
                    .ToList();

                return new AdminUserActivityDto
                {
                    Username = user.Username,
                    Role = user.Role,
                    IsActive = user.IsActive,
                    CreatedAt = user.CreatedAt,
                    LastLoginAt = user.LastLoginAt,
                    ScansCount = userScans.Count,
                    LastScanAt = userScans.FirstOrDefault()?.CreatedAt,
                    Scans = userScans.Select(scan => new AdminUserScanItemDto
                    {
                        Id = scan.Id,
                        TargetName = scan.TargetName,
                        OpenApiUrl = scan.OpenApiUrl,
                        Score = scan.Score,
                        Status = scan.Status.ToString(),
                        CreatedAt = scan.CreatedAt,
                        IssuesCount = scan.SecurityIssues.Count
                    }).ToList()
                };
            })
            .ToList();
    }
}
