using Cloudsoft.Api.Dtos;
using Cloudsoft.Core.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Cloudsoft.Api.Controllers;

[ApiController]
[Route("api/jobs")]
public class JobsController : ControllerBase
{
    private readonly IJobPostingService _jobPostingService;

    public JobsController(IJobPostingService jobPostingService)
    {
        _jobPostingService = jobPostingService;
    }

    [HttpGet]
    public async Task<IReadOnlyCollection<JobPostingDto>> GetAll()
    {
        var jobs = await _jobPostingService.GetAllAsync();
        return jobs.Select(job => job.ToDto()).ToList();
    }

    [HttpGet("active")]
    public async Task<IReadOnlyCollection<JobPostingDto>> GetActive()
    {
        var jobs = await _jobPostingService.GetActiveAsync();
        return jobs.Select(job => job.ToDto()).ToList();
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<JobPostingDto>> GetById(string id)
    {
        var jobPosting = await _jobPostingService.GetByIdAsync(id);
        return jobPosting is null ? NotFound() : Ok(jobPosting.ToDto());
    }

    [HttpPost]
    [Authorize]
    public async Task<ActionResult<JobPostingDto>> Create(CreateJobPostingDto dto)
    {
        var jobPosting = dto.ToModel();
        var createdJobPosting = await _jobPostingService.CreateAsync(jobPosting);

        return CreatedAtAction(
            nameof(GetById),
            new { id = createdJobPosting.Id },
            createdJobPosting.ToDto());
    }
}
