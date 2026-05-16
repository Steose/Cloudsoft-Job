using Cloudsoft.Core.Models;
using Cloudsoft.Core.Repositories.Interfaces;
using Cloudsoft.Core.Services.Interfaces;

namespace Cloudsoft.Core.Services;

public class JobPostingService : IJobPostingService
{
    private readonly IJobPostingRepository _jobPostingRepository;

    public JobPostingService(IJobPostingRepository jobPostingRepository)
    {
        _jobPostingRepository = jobPostingRepository;
    }

    public Task<IReadOnlyCollection<JobPosting>> GetAllAsync()
    {
        return _jobPostingRepository.GetAllAsync();
    }

    public Task<IReadOnlyCollection<JobPosting>> GetActiveAsync()
    {
        return _jobPostingRepository.GetActiveAsync();
    }

    public Task<IReadOnlyCollection<JobPosting>> GetByEmployerIdAsync(string employerId)
    {
        return _jobPostingRepository.GetByEmployerIdAsync(employerId);
    }

    public Task<JobPosting?> GetByIdAsync(string id)
    {
        return _jobPostingRepository.GetByIdAsync(id);
    }

    public async Task<JobPosting> CreateAsync(JobPosting jobPosting)
    {
        ArgumentNullException.ThrowIfNull(jobPosting);

        if (string.IsNullOrWhiteSpace(jobPosting.Id))
        {
            jobPosting.Id = Guid.NewGuid().ToString();
        }

        if (jobPosting.CreatedAtUtc == default)
        {
            jobPosting.CreatedAtUtc = DateTime.UtcNow;
        }

        return await _jobPostingRepository.AddAsync(jobPosting);
    }

    public Task<bool> UpdateAsync(JobPosting jobPosting)
    {
        return _jobPostingRepository.UpdateAsync(jobPosting);
    }

    public Task<bool> DeleteAsync(string id)
    {
        return _jobPostingRepository.DeleteAsync(id);
    }

    public async Task<bool> ToggleIsActiveAsync(string id)
    {
        var jobPosting = await _jobPostingRepository.GetByIdAsync(id);
        if (jobPosting == null)
        {
            return false;
        }

        jobPosting.IsActive = !jobPosting.IsActive;

        return await _jobPostingRepository.UpdateAsync(jobPosting);
    }
}
