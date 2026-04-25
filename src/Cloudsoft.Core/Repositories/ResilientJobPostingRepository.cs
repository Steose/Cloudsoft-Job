using Cloudsoft.Core.Data;
using Cloudsoft.Core.Models;
using Cloudsoft.Core.Repositories.Interfaces;
using Microsoft.Extensions.Logging;

namespace Cloudsoft.Core.Repositories;

public class ResilientJobPostingRepository : IJobPostingRepository
{
    private readonly MongoJobPostingRepository _mongoRepository;
    private readonly JobPostingRepository _fallbackRepository;
    private readonly IInMemoryDatabase _database;
    private readonly ILogger<ResilientJobPostingRepository> _logger;

    public ResilientJobPostingRepository(
        MongoJobPostingRepository mongoRepository,
        JobPostingRepository fallbackRepository,
        IInMemoryDatabase database,
        ILogger<ResilientJobPostingRepository> logger)
    {
        _mongoRepository = mongoRepository;
        _fallbackRepository = fallbackRepository;
        _database = database;
        _logger = logger;
    }

    public async Task<IReadOnlyCollection<JobPosting>> GetAllAsync()
    {
        try
        {
            var jobPostings = await _mongoRepository.GetAllAsync();
            SyncCache(jobPostings);
            return jobPostings;
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "MongoDb job repository failed during {Operation}. Falling back to in-memory storage.",
                nameof(GetAllAsync));

            return await _fallbackRepository.GetAllAsync();
        }
    }

    public async Task<IReadOnlyCollection<JobPosting>> GetActiveAsync()
    {
        try
        {
            var jobPostings = await _mongoRepository.GetActiveAsync();
            SyncCache(jobPostings);
            return jobPostings;
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "MongoDb job repository failed during {Operation}. Falling back to in-memory storage.",
                nameof(GetActiveAsync));

            return await _fallbackRepository.GetActiveAsync();
        }
    }

    public async Task<JobPosting?> GetByIdAsync(string id)
    {
        try
        {
            var jobPosting = await _mongoRepository.GetByIdAsync(id);
            if (jobPosting != null)
            {
                _database.JobPostings[jobPosting.Id] = Clone(jobPosting);
            }

            return jobPosting;
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "MongoDb job repository failed during {Operation}. Falling back to in-memory storage.",
                nameof(GetByIdAsync));

            return await _fallbackRepository.GetByIdAsync(id);
        }
    }

    public async Task<JobPosting> AddAsync(JobPosting jobPosting)
    {
        try
        {
            var createdJobPosting = await _mongoRepository.AddAsync(jobPosting);
            _database.JobPostings[createdJobPosting.Id] = Clone(createdJobPosting);
            return createdJobPosting;
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "MongoDb job repository failed during {Operation}. Falling back to in-memory storage.",
                nameof(AddAsync));

            return await _fallbackRepository.AddAsync(jobPosting);
        }
    }

    public async Task<bool> UpdateAsync(JobPosting jobPosting)
    {
        try
        {
            var updated = await _mongoRepository.UpdateAsync(jobPosting);
            if (updated)
            {
                _database.JobPostings[jobPosting.Id] = Clone(jobPosting);
            }

            return updated;
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "MongoDb job repository failed during {Operation}. Falling back to in-memory storage.",
                nameof(UpdateAsync));

            return await _fallbackRepository.UpdateAsync(jobPosting);
        }
    }

    public async Task<bool> DeleteAsync(string id)
    {
        try
        {
            var deleted = await _mongoRepository.DeleteAsync(id);
            if (deleted)
            {
                _database.JobPostings.TryRemove(id, out _);
            }

            return deleted;
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "MongoDb job repository failed during {Operation}. Falling back to in-memory storage.",
                nameof(DeleteAsync));

            return await _fallbackRepository.DeleteAsync(id);
        }
    }

    private void SyncCache(IReadOnlyCollection<JobPosting> jobPostings)
    {
        foreach (var jobPosting in jobPostings)
        {
            _database.JobPostings[jobPosting.Id] = Clone(jobPosting);
        }
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
