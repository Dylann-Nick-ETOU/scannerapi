using ApiSecurityScanner.Domain.Entities;
using ApiSecurityScanner.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace ApiSecurityScanner.Infrastructure.Persistence.Repositories;

public class UserRepository(ApiSecurityScannerDbContext dbContext) : IUserRepository
{
    public async Task<IReadOnlyList<AppUser>> GetAllAsync(CancellationToken cancellationToken = default) =>
        await dbContext.AppUsers.OrderBy(x => x.Username).ToListAsync(cancellationToken);

    public async Task<AppUser?> GetByUsernameAsync(string username, CancellationToken cancellationToken = default) =>
        await dbContext.AppUsers.FirstOrDefaultAsync(
            x => x.Username.ToLower() == username.ToLower(),
            cancellationToken);

    public async Task<bool> ExistsByUsernameAsync(string username, CancellationToken cancellationToken = default) =>
        await dbContext.AppUsers.AnyAsync(
            x => x.Username.ToLower() == username.ToLower(),
            cancellationToken);

    public async Task<bool> DeactivateAsync(string username, CancellationToken cancellationToken = default)
    {
        var user = await GetByUsernameAsync(username, cancellationToken);
        if (user is null)
        {
            return false;
        }

        user.IsActive = false;
        return true;
    }

    public async Task<bool> ReactivateAsync(string username, CancellationToken cancellationToken = default)
    {
        var user = await GetByUsernameAsync(username, cancellationToken);
        if (user is null)
        {
            return false;
        }

        user.IsActive = true;
        return true;
    }

    public async Task AddAsync(AppUser user, CancellationToken cancellationToken = default) =>
        await dbContext.AppUsers.AddAsync(user, cancellationToken);

    public async Task SaveChangesAsync(CancellationToken cancellationToken = default) =>
        await dbContext.SaveChangesAsync(cancellationToken);
}
