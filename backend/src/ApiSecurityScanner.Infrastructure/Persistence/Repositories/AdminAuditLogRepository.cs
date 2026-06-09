using ApiSecurityScanner.Domain.Entities;
using ApiSecurityScanner.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace ApiSecurityScanner.Infrastructure.Persistence.Repositories;

public class AdminAuditLogRepository(ApiSecurityScannerDbContext dbContext) : IAdminAuditLogRepository
{
    public async Task<AdminAuditLog?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        await dbContext.AdminAuditLogs.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

    public async Task<IReadOnlyList<AdminAuditLog>> GetAllAsync(CancellationToken cancellationToken = default) =>
        await dbContext.AdminAuditLogs
            .OrderByDescending(x => x.CreatedAt)
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<AdminAuditLog>> GetRecentAsync(int top, CancellationToken cancellationToken = default) =>
        await dbContext.AdminAuditLogs
            .OrderByDescending(x => x.CreatedAt)
            .Take(top)
            .ToListAsync(cancellationToken);

    public async Task AddAsync(AdminAuditLog entity, CancellationToken cancellationToken = default) =>
        await dbContext.AdminAuditLogs.AddAsync(entity, cancellationToken);

    public Task DeleteAsync(AdminAuditLog entity, CancellationToken cancellationToken = default)
    {
        dbContext.AdminAuditLogs.Remove(entity);
        return Task.CompletedTask;
    }

    public async Task SaveChangesAsync(CancellationToken cancellationToken = default) =>
        await dbContext.SaveChangesAsync(cancellationToken);
}
