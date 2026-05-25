using Cloudsoft.Core.Models;

namespace Cloudsoft.Api.Dtos;

public static class JobPostingDtoMapping
{
    public static JobPostingDto ToDto(this JobPosting jobPosting)
    {
        return new JobPostingDto
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

    public static JobPosting ToModel(this CreateJobPostingDto dto)
    {
        return new JobPosting
        {
            Title = dto.Title,
            Description = dto.Description,
            Location = dto.Location,
            Deadline = dto.Deadline,
            IsActive = dto.IsActive
        };
    }
}