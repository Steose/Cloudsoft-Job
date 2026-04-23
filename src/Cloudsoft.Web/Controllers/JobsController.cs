
using Cloudsoft.Core.Models;
using Cloudsoft.Core.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Cloudsoft.Web.Controllers;

[Route("Job")]
public class JobsController : Controller
{
    private readonly IJobPostingService _jobPostingService;

    public JobsController(IJobPostingService jobPostingService)
    {
        _jobPostingService = jobPostingService;
    }

    [HttpGet("")]
    public async Task<IActionResult> Index()
    {
        var jobPostings = await _jobPostingService.GetAllAsync();
        return View("JobPostings", jobPostings);
    }

    [HttpGet("/Jobs/JobPostings")]
    public IActionResult JobPostings()
    {
        return RedirectToAction(nameof(Index));
    }

    [HttpGet("/Jobs/JobPosting")]
    public IActionResult JobPosting()
    {
        return RedirectToAction(nameof(Create));
    }

    [HttpGet("Details/{id}")]
    public async Task<IActionResult> Details(string id)
    {
        var jobPosting = await _jobPostingService.GetByIdAsync(id);
        if (jobPosting == null)
        {
            return NotFound();
        }

        return View(jobPosting);
    }

    [Authorize]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ToggleIsActive(string id)
    {
        var jobPosting = await _jobPostingService.GetByIdAsync(id);
        if (jobPosting == null)
        {
            return NotFound();
        }

        var willBeActive = !jobPosting.IsActive;
        if (!await _jobPostingService.ToggleIsActiveAsync(id))
        {
            return NotFound();
        }

        TempData["SuccessMessage"] = willBeActive
            ? $"Job '{jobPosting.Title}' is now active."
            : $"Job '{jobPosting.Title}' has been deactivated.";

        return RedirectToAction(nameof(JobPostings));
    }

    [Authorize]
    [HttpGet("Create")]
    public IActionResult Create()
    {
        return View("JobPosting", new JobPosting());
    }

    [Authorize]
    [HttpPost("Create")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(JobPosting jobPosting)
    {
        if (jobPosting.Deadline == default)
        {
            ModelState.AddModelError(nameof(jobPosting.Deadline), "The application deadline is required.");
        }

        if (jobPosting.Deadline.Date < DateTime.UtcNow.Date)
        {
            ModelState.AddModelError(nameof(jobPosting.Deadline), "The application deadline cannot be in the past.");
        }

        if (!ModelState.IsValid)
        {
            return View("JobPosting", jobPosting);
        }

        await _jobPostingService.CreateAsync(jobPosting);
        TempData["SuccessMessage"] = $"Thank you for posting the {jobPosting.Title} job!";

        return RedirectToAction(nameof(Index));
    }
}
