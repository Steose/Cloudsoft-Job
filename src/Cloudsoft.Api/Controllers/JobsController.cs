using Cloudsoft.Core.Models;
using Cloudsoft.Core.Services.Interfaces;
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
    public Task<IReadOnlyCollection<JobPosting>> GetAll()
    {
        return _jobPostingService.GetAllAsync();
    }

    [HttpGet("active")]
    public Task<IReadOnlyCollection<JobPosting>> GetActive()
    {
        return _jobPostingService.GetActiveAsync();
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<JobPosting>> GetById(string id)
    {
        var jobPosting = await _jobPostingService.GetByIdAsync(id);
        return jobPosting is null ? NotFound() : Ok(jobPosting);
    }

    [HttpPost]
    public async Task<ActionResult<JobPosting>> Create(JobPosting jobPosting)
    {
        var createdJobPosting = await _jobPostingService.CreateAsync(jobPosting);
        return CreatedAtAction(nameof(GetById), new { id = createdJobPosting.Id }, createdJobPosting);
    }
}
