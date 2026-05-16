using Cloudsoft.Core.Data;
using Cloudsoft.Core.Models;
using Cloudsoft.Core.Repositories.Interfaces;

namespace Cloudsoft.Core.Repositories;

public class JobApplicationRepository : IJobApplicationRepository
{
    private readonly IInMemoryDatabase _database;

    public JobApplicationRepository(IInMemoryDatabase database)
    {
        _database = database;
    }

    public Task<IReadOnlyCollection<JobApplication>> GetAllAsync()
    {
        IReadOnlyCollection<JobApplication> applications = _database.JobApplications.Values
            .OrderByDescending(application => application.SubmittedAtUtc)
            .Select(Clone)
            .ToList();

        return Task.FromResult(applications);
    }

    public Task<IReadOnlyCollection<JobApplication>> GetByJobPostingIdAsync(string jobPostingId)
    {
        IReadOnlyCollection<JobApplication> applications = _database.JobApplications.Values
            .Where(application => application.JobPostingId == jobPostingId)
            .OrderByDescending(application => application.SubmittedAtUtc)
            .Select(Clone)
            .ToList();

        return Task.FromResult(applications);
    }

    public Task<bool> ExistsForJobAndEmailAsync(string jobPostingId, string email)
    {
        var normalizedEmail = NormalizeEmail(email);
        var exists = _database.JobApplications.Values.Any(application =>
            application.JobPostingId == jobPostingId &&
            NormalizeEmail(application.Email) == normalizedEmail);

        return Task.FromResult(exists);
    }

    public Task<JobApplication> AddAsync(JobApplication application)
    {
        ArgumentNullException.ThrowIfNull(application);

        var storedApplication = Clone(application);
        _database.JobApplications[storedApplication.Id] = storedApplication;

        return Task.FromResult(Clone(storedApplication));
    }

    private static string NormalizeEmail(string email)
    {
        return email.Trim().ToUpperInvariant();
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
