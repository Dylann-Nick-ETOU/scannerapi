using ApiSecurityScanner.Domain.Interfaces;

namespace ApiSecurityScanner.Application.UseCases;

public class DeactivateUserUseCase(IUserRepository userRepository)
{
    public async Task<bool> ExecuteAsync(string username, CancellationToken cancellationToken = default)
    {
        var deactivated = await userRepository.DeactivateAsync(username, cancellationToken);
        if (!deactivated)
        {
            return false;
        }

        await userRepository.SaveChangesAsync(cancellationToken);
        return true;
    }
}
