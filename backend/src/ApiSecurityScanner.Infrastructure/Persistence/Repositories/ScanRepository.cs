using ApiSecurityScanner.Domain.Entities;
using ApiSecurityScanner.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace ApiSecurityScanner.Infrastructure.Persistence.Repositories;

public class ScanRepository(ApiSecurityScannerDbContext dbContext) : IScanRepository
{
    public async Task<Scan?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        await dbContext.Scans.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

    public async Task<IReadOnlyList<Scan>> GetAllAsync(CancellationToken cancellationToken = default) =>
        await dbContext.Scans.OrderByDescending(x => x.CreatedAt).ToListAsync(cancellationToken);

    public async Task AddAsync(Scan entity, CancellationToken cancellationToken = default) =>
        await dbContext.Scans.AddAsync(entity, cancellationToken);

    public Task DeleteAsync(Scan entity, CancellationToken cancellationToken = default)
    {
        dbContext.Scans.Remove(entity);
        return Task.CompletedTask;
    }

    public async Task SaveChangesAsync(CancellationToken cancellationToken = default) =>
        await dbContext.SaveChangesAsync(cancellationToken);

    public async Task<Scan?> GetWithIssuesAsync(Guid id, CancellationToken cancellationToken = default) =>
        await dbContext.Scans.Include(x => x.SecurityIssues).FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
}
