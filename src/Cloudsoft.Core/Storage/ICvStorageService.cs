namespace Cloudsoft.Core.Storage;

public interface ICvStorageService
{
    Task<string> SaveAsync(Stream cvStream, string originalFileName, CancellationToken cancellationToken = default);
}
