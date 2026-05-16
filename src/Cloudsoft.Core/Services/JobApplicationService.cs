using Cloudsoft.Core.Models;
using Cloudsoft.Core.Repositories.Interfaces;
using Cloudsoft.Core.Services.Interfaces;
using Cloudsoft.Core.Storage;

namespace Cloudsoft.Core.Services;

public class JobApplicationService : IJobApplicationService
{
    private readonly IJobApplicationRepository _applicationRepository;
    private readonly IJobPostingRepository _jobPostingRepository;
    private readonly ICountryLookupService _countryLookupService;
    private readonly ICvStorageService _cvStorageService;

    public JobApplicationService(
        IJobApplicationRepository applicationRepository,
        IJobPostingRepository jobPostingRepository,
        ICountryLookupService countryLookupService,
        ICvStorageService cvStorageService)
    {
        _applicationRepository = applicationRepository;
        _jobPostingRepository = jobPostingRepository;
        _countryLookupService = countryLookupService;
        _cvStorageService = cvStorageService;
    }

    public Task<IReadOnlyCollection<JobApplication>> GetAllAsync()
    {
        return _applicationRepository.GetAllAsync();
    }

    public Task<IReadOnlyCollection<JobApplication>> GetByJobPostingIdAsync(string jobPostingId)
    {
        return _applicationRepository.GetByJobPostingIdAsync(jobPostingId);
    }

    public async Task<JobApplication> SubmitAsync(
        JobApplication application,
        Stream cvStream,
        string originalFileName,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(application);
        ArgumentNullException.ThrowIfNull(cvStream);

        application.FullName = application.FullName.Trim();
        application.Email = application.Email.Trim();
        application.CountryCode = application.CountryCode.Trim().ToUpperInvariant();

        if (string.IsNullOrWhiteSpace(application.JobPostingId))
        {
            throw new InvalidOperationException("The selected job posting is required.");
        }

        if (string.IsNullOrWhiteSpace(application.FullName))
        {
            throw new InvalidOperationException("Your full name is required.");
        }

        if (string.IsNullOrWhiteSpace(application.Email))
        {
            throw new InvalidOperationException("Your email address is required.");
        }

        if (!_countryLookupService.IsKnownCountryCode(application.CountryCode))
        {
            throw new InvalidOperationException("Select a valid country.");
        }

        var jobPosting = await _jobPostingRepository.GetByIdAsync(application.JobPostingId);
        if (jobPosting == null)
        {
            throw new InvalidOperationException("The selected job posting could not be found.");
        }

        if (!jobPosting.IsActive || jobPosting.Deadline.Date < DateTime.UtcNow.Date)
        {
            throw new InvalidOperationException("This job posting is no longer accepting applications.");
        }

        if (await _applicationRepository.ExistsForJobAndEmailAsync(application.JobPostingId, application.Email))
        {
            throw new InvalidOperationException("An application has already been submitted for this job using that email address.");
        }

        if (string.IsNullOrWhiteSpace(application.Id))
        {
            application.Id = Guid.NewGuid().ToString();
        }

        if (application.SubmittedAtUtc == default)
        {
            application.SubmittedAtUtc = DateTime.UtcNow;
        }

        application.CvFileName = await _cvStorageService.SaveAsync(cvStream, originalFileName, cancellationToken);

        return await _applicationRepository.AddAsync(application);
    }
}
