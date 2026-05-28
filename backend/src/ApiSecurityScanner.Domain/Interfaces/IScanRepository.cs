using ApiSecurityScanner.Domain.Entities;

namespace ApiSecurityScanner.Domain.Interfaces;

public interface IScanRepository : IRepository<Scan>
{
    Task<Scan?> GetWithIssuesAsync(Guid id, CancellationToken cancellationToken = default);
}
