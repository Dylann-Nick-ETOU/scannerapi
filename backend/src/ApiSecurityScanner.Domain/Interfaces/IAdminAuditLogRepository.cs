using ApiSecurityScanner.Domain.Entities;

namespace ApiSecurityScanner.Domain.Interfaces;

public interface IAdminAuditLogRepository : IRepository<AdminAuditLog>
{
    Task<IReadOnlyList<AdminAuditLog>> GetRecentAsync(int top, CancellationToken cancellationToken = default);
}
