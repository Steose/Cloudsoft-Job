using Cloudsoft.Core.Models;

namespace Cloudsoft.Core.Repositories.Interfaces;

public interface IJobApplicationRepository
{
    Task<IReadOnlyCollection<JobApplication>> GetAllAsync();

    Task<IReadOnlyCollection<JobApplication>> GetByJobPostingIdAsync(string jobPostingId);

    Task<bool> ExistsForJobAndEmailAsync(string jobPostingId, string email);

    Task<JobApplication> AddAsync(JobApplication application);
}
