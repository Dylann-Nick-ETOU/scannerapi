using ApiSecurityScanner.Application.DTOs;
using ApiSecurityScanner.Domain.Interfaces;

namespace ApiSecurityScanner.Application.UseCases;

public class GetAdminAuditLogsUseCase(IAdminAuditLogRepository adminAuditLogRepository)
{
    public async Task<IReadOnlyList<AdminAuditLogDto>> ExecuteAsync(int top = 50, CancellationToken cancellationToken = default)
    {
        var logs = await adminAuditLogRepository.GetRecentAsync(top, cancellationToken);
        return logs.Select(x => x.ToDto()).ToList();
    }
}
