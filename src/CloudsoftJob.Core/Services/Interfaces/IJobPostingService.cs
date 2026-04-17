using CloudsoftJob.Core.Models;

namespace CloudsoftJob.Core.Services.Interfaces;

public interface IJobPostingService
{
    IReadOnlyCollection<JobPosting> GetAll();

    IReadOnlyCollection<JobPosting> GetActive();

    JobPosting? GetById(string id);

    JobPosting Create(JobPosting jobPosting);

    bool Update(JobPosting jobPosting);

    bool Delete(string id);

    bool ToggleIsActive(string id);
}
