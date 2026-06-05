using ApiSecurityScanner.Domain.Interfaces;

namespace ApiSecurityScanner.Application.UseCases;

public class DeleteScanUseCase(IScanRepository scanRepository)
{
    public async Task<bool> ExecuteAsync(Guid id, string ownerId, CancellationToken cancellationToken = default)
    {
        var scan = await scanRepository.GetByIdForOwnerAsync(id, ownerId, cancellationToken);
        if (scan is null)
        {
            return false;
        }

        await scanRepository.DeleteAsync(scan, cancellationToken);
        await scanRepository.SaveChangesAsync(cancellationToken);
        return true;
    }
}
