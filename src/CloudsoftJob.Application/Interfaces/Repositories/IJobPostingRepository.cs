using CloudsoftJob.Core.Models;

namespace CloudsoftJob.Core.Repositories.Interfaces;

public interface IJobPostingRepository
{
    Task<IReadOnlyCollection<JobPosting>> GetAllAsync();

    Task<IReadOnlyCollection<JobPosting>> GetActiveAsync();

    Task<JobPosting?> GetByIdAsync(string id);

    Task<JobPosting> AddAsync(JobPosting jobPosting);

    Task<bool> UpdateAsync(JobPosting jobPosting);

    Task<bool> DeleteAsync(string id);
}
