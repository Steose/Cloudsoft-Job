using Cloudsoft.Core.Models;

namespace Cloudsoft.Core.Repositories.Interfaces;

public interface IJobPostingRepository
{
    Task<IReadOnlyCollection<JobPosting>> GetAllAsync();

    Task<IReadOnlyCollection<JobPosting>> GetActiveAsync();

    Task<IReadOnlyCollection<JobPosting>> GetByEmployerIdAsync(string employerId);

    Task<JobPosting?> GetByIdAsync(string id);

    Task<JobPosting> AddAsync(JobPosting jobPosting);

    Task<bool> UpdateAsync(JobPosting jobPosting);

    Task<bool> DeleteAsync(string id);
}
