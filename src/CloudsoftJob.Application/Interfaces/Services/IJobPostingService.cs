using CloudsoftJob.Core.Models;

namespace CloudsoftJob.Core.Services.Interfaces;

public interface IJobPostingService
{
    Task<IReadOnlyCollection<JobPosting>> GetAllAsync();

    Task<IReadOnlyCollection<JobPosting>> GetActiveAsync();

    Task<JobPosting?> GetByIdAsync(string id);

    Task<JobPosting> CreateAsync(JobPosting jobPosting);

    Task<bool> UpdateAsync(JobPosting jobPosting);

    Task<bool> DeleteAsync(string id);

    Task<bool> ToggleIsActiveAsync(string id);
}
