using CloudsoftJob.Core.Models;
using CloudsoftJob.Core.Services.Interfaces;

namespace CloudsoftJob.Core.Services;

public class JobPostingService : IJobPostingService
{
    private readonly List<JobPosting> _jobPostings = [];
    private readonly object _syncRoot = new();

    public IReadOnlyCollection<JobPosting> GetAll()
    {
        lock (_syncRoot)
        {
            return _jobPostings.ToList();
        }
    }

    public IReadOnlyCollection<JobPosting> GetActive()
    {
        lock (_syncRoot)
        {
            return _jobPostings
                .Where(jobPosting => jobPosting.IsActive)
                .ToList();
        }
    }

    public JobPosting? GetById(string id)
    {
        if (string.IsNullOrWhiteSpace(id))
        {
            return null;
        }

        lock (_syncRoot)
        {
            return _jobPostings.FirstOrDefault(jobPosting => jobPosting.Id == id);
        }
    }

    public JobPosting Create(JobPosting jobPosting)
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

        if (jobPosting.Deadline == default)
        {
            jobPosting.Deadline = DateTime.UtcNow.Date.AddDays(21);
        }

        lock (_syncRoot)
        {
            _jobPostings.Add(jobPosting);
        }

        return jobPosting;
    }

    public bool Update(JobPosting jobPosting)
    {
        ArgumentNullException.ThrowIfNull(jobPosting);

        lock (_syncRoot)
        {
            var existingJobPosting = _jobPostings.FirstOrDefault(existing => existing.Id == jobPosting.Id);
            if (existingJobPosting == null)
            {
                return false;
            }

            existingJobPosting.Title = jobPosting.Title;
            existingJobPosting.Description = jobPosting.Description;
            existingJobPosting.Location = jobPosting.Location;
            existingJobPosting.CreatedAtUtc = jobPosting.CreatedAtUtc;
            existingJobPosting.Deadline = jobPosting.Deadline;
            existingJobPosting.IsActive = jobPosting.IsActive;

            return true;
        }
    }

    public bool Delete(string id)
    {
        if (string.IsNullOrWhiteSpace(id))
        {
            return false;
        }

        lock (_syncRoot)
        {
            var jobPosting = _jobPostings.FirstOrDefault(existing => existing.Id == id);
            if (jobPosting == null)
            {
                return false;
            }

            _jobPostings.Remove(jobPosting);
            return true;
        }
    }

    public bool ToggleIsActive(string id)
    {
        if (string.IsNullOrWhiteSpace(id))
        {
            return false;
        }

        lock (_syncRoot)
        {
            var jobPosting = _jobPostings.FirstOrDefault(existing => existing.Id == id);
            if (jobPosting == null)
            {
                return false;
            }

            jobPosting.IsActive = !jobPosting.IsActive;
            return true;
        }
    }
}
