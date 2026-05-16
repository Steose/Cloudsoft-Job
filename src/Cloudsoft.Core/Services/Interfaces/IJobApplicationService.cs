using Cloudsoft.Core.Models;

namespace Cloudsoft.Core.Services.Interfaces;

public interface IJobApplicationService
{
    Task<IReadOnlyCollection<JobApplication>> GetAllAsync();

    Task<IReadOnlyCollection<JobApplication>> GetByJobPostingIdAsync(string jobPostingId);

    Task<JobApplication> SubmitAsync(
        JobApplication application,
        Stream cvStream,
        string originalFileName,
        CancellationToken cancellationToken = default);
}
