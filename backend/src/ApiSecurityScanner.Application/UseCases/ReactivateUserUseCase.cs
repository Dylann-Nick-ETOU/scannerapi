using ApiSecurityScanner.Domain.Interfaces;

namespace ApiSecurityScanner.Application.UseCases;

public class ReactivateUserUseCase(IUserRepository userRepository)
{
    public async Task<bool> ExecuteAsync(string username, CancellationToken cancellationToken = default)
    {
        var reactivated = await userRepository.ReactivateAsync(username, cancellationToken);
        if (!reactivated)
        {
            return false;
        }

        await userRepository.SaveChangesAsync(cancellationToken);
        return true;
    }
}
