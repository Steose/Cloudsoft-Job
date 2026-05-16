using Cloudsoft.Core.Models;

namespace Cloudsoft.Core.Services.Interfaces;

public interface IJobPostingService
{
    Task<IReadOnlyCollection<JobPosting>> GetAllAsync();

    Task<IReadOnlyCollection<JobPosting>> GetActiveAsync();

    Task<IReadOnlyCollection<JobPosting>> GetByEmployerIdAsync(string employerId);

    Task<JobPosting?> GetByIdAsync(string id);

    Task<JobPosting> CreateAsync(JobPosting jobPosting);

    Task<bool> UpdateAsync(JobPosting jobPosting);

    Task<bool> DeleteAsync(string id);

    Task<bool> ToggleIsActiveAsync(string id);
}
