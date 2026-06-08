using ApiSecurityScanner.Domain.Entities;

namespace ApiSecurityScanner.Domain.Interfaces;

public interface IUserRepository
{
    Task<AppUser?> GetByUsernameAsync(string username, CancellationToken cancellationToken = default);
    Task<bool> ExistsByUsernameAsync(string username, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<AppUser>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<bool> DeactivateAsync(string username, CancellationToken cancellationToken = default);
    Task<bool> ReactivateAsync(string username, CancellationToken cancellationToken = default);
    Task AddAsync(AppUser user, CancellationToken cancellationToken = default);
    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
