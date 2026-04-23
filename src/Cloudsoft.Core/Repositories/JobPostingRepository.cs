using Cloudsoft.Core.Data;
using Cloudsoft.Core.Models;
using Cloudsoft.Core.Repositories.Interfaces;

namespace Cloudsoft.Core.Repositories;

public class JobPostingRepository : IJobPostingRepository
{
    private readonly IInMemoryDatabase _database;

    public JobPostingRepository(IInMemoryDatabase database)
    {
        _database = database;
    }

    public Task<IReadOnlyCollection<JobPosting>> GetAllAsync()
    {
        IReadOnlyCollection<JobPosting> jobPostings = _database.JobPostings.Values
            .OrderByDescending(jobPosting => jobPosting.CreatedAtUtc)
            .Select(Clone)
            .ToList();

        return Task.FromResult(jobPostings);
    }

    public Task<IReadOnlyCollection<JobPosting>> GetActiveAsync()
    {
        IReadOnlyCollection<JobPosting> jobPostings = _database.JobPostings.Values
            .Where(jobPosting => jobPosting.IsActive)
            .OrderByDescending(jobPosting => jobPosting.CreatedAtUtc)
            .Select(Clone)
            .ToList();

        return Task.FromResult(jobPostings);
    }

    public Task<JobPosting?> GetByIdAsync(string id)
    {
        if (string.IsNullOrWhiteSpace(id))
        {
            return Task.FromResult<JobPosting?>(null);
        }

        return Task.FromResult(
            _database.JobPostings.TryGetValue(id, out var jobPosting)
                ? Clone(jobPosting)
                : null);
    }

    public Task<JobPosting> AddAsync(JobPosting jobPosting)
    {
        ArgumentNullException.ThrowIfNull(jobPosting);

        var storedJobPosting = Clone(jobPosting);
        _database.JobPostings[storedJobPosting.Id] = storedJobPosting;

        return Task.FromResult(Clone(storedJobPosting));
    }

    public Task<bool> UpdateAsync(JobPosting jobPosting)
    {
        ArgumentNullException.ThrowIfNull(jobPosting);

        if (string.IsNullOrWhiteSpace(jobPosting.Id))
        {
            return Task.FromResult(false);
        }

        while (_database.JobPostings.TryGetValue(jobPosting.Id, out var existingJobPosting))
        {
            if (_database.JobPostings.TryUpdate(jobPosting.Id, Clone(jobPosting), existingJobPosting))
            {
                return Task.FromResult(true);
            }
        }

        return Task.FromResult(false);
    }

    public Task<bool> DeleteAsync(string id)
    {
        if (string.IsNullOrWhiteSpace(id))
        {
            return Task.FromResult(false);
        }

        return Task.FromResult(_database.JobPostings.TryRemove(id, out _));
    }

    private static JobPosting Clone(JobPosting jobPosting)
    {
        return new JobPosting
        {
            Id = jobPosting.Id,
            Title = jobPosting.Title,
            Description = jobPosting.Description,
            Location = jobPosting.Location,
            CreatedAtUtc = jobPosting.CreatedAtUtc,
            Deadline = jobPosting.Deadline,
            IsActive = jobPosting.IsActive
        };
    }
}
