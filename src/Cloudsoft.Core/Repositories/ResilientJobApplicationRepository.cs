using Cloudsoft.Core.Data;
using Cloudsoft.Core.Models;
using Cloudsoft.Core.Repositories.Interfaces;
using Microsoft.Extensions.Logging;

namespace Cloudsoft.Core.Repositories;

public class ResilientJobApplicationRepository : IJobApplicationRepository
{
    private readonly MongoJobApplicationRepository _mongoRepository;
    private readonly JobApplicationRepository _fallbackRepository;
    private readonly IInMemoryDatabase _database;
    private readonly ILogger<ResilientJobApplicationRepository> _logger;

    public ResilientJobApplicationRepository(
        MongoJobApplicationRepository mongoRepository,
        JobApplicationRepository fallbackRepository,
        IInMemoryDatabase database,
        ILogger<ResilientJobApplicationRepository> logger)
    {
        _mongoRepository = mongoRepository;
        _fallbackRepository = fallbackRepository;
        _database = database;
        _logger = logger;
    }

    public async Task<IReadOnlyCollection<JobApplication>> GetAllAsync()
    {
        try
        {
            var applications = await _mongoRepository.GetAllAsync();
            SyncCache(applications);
            return applications;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "MongoDb application repository failed during {Operation}. Falling back to in-memory storage.", nameof(GetAllAsync));
            return await _fallbackRepository.GetAllAsync();
        }
    }

    public async Task<IReadOnlyCollection<JobApplication>> GetByJobPostingIdAsync(string jobPostingId)
    {
        try
        {
            var applications = await _mongoRepository.GetByJobPostingIdAsync(jobPostingId);
            SyncCache(applications);
            return applications;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "MongoDb application repository failed during {Operation}. Falling back to in-memory storage.", nameof(GetByJobPostingIdAsync));
            return await _fallbackRepository.GetByJobPostingIdAsync(jobPostingId);
        }
    }

    public async Task<bool> ExistsForJobAndEmailAsync(string jobPostingId, string email)
    {
        try
        {
            return await _mongoRepository.ExistsForJobAndEmailAsync(jobPostingId, email);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "MongoDb application repository failed during {Operation}. Falling back to in-memory storage.", nameof(ExistsForJobAndEmailAsync));
            return await _fallbackRepository.ExistsForJobAndEmailAsync(jobPostingId, email);
        }
    }

    public async Task<JobApplication> AddAsync(JobApplication application)
    {
        try
        {
            var createdApplication = await _mongoRepository.AddAsync(application);
            _database.JobApplications[createdApplication.Id] = Clone(createdApplication);
            return createdApplication;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "MongoDb application repository failed during {Operation}. Falling back to in-memory storage.", nameof(AddAsync));
            return await _fallbackRepository.AddAsync(application);
        }
    }

    private void SyncCache(IReadOnlyCollection<JobApplication> applications)
    {
        foreach (var application in applications)
        {
            _database.JobApplications[application.Id] = Clone(application);
        }
    }

    private static JobApplication Clone(JobApplication application)
    {
        return new JobApplication
        {
            Id = application.Id,
            JobPostingId = application.JobPostingId,
            FullName = application.FullName,
            Email = application.Email,
            CountryCode = application.CountryCode,
            CvFileName = application.CvFileName,
            SubmittedAtUtc = application.SubmittedAtUtc
        };
    }
}
