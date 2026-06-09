using ApiSecurityScanner.Domain.Entities;
using ApiSecurityScanner.Domain.Interfaces;

namespace ApiSecurityScanner.Application.UseCases;

public class RecordAdminAuditLogUseCase(IAdminAuditLogRepository adminAuditLogRepository)
{
    public async Task ExecuteAsync(
        string adminUsername,
        string actionType,
        string? targetUsername,
        Guid? targetScanId,
        string details,
        CancellationToken cancellationToken = default)
    {
        await adminAuditLogRepository.AddAsync(new AdminAuditLog
        {
            AdminUsername = adminUsername,
            ActionType = actionType,
            TargetUsername = targetUsername,
            TargetScanId = targetScanId,
            Details = details
        }, cancellationToken);

        await adminAuditLogRepository.SaveChangesAsync(cancellationToken);
    }
}
