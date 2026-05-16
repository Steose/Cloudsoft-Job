using System.Security.Claims;
using Cloudsoft.Core.Models;
using Cloudsoft.Core.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Cloudsoft.Web.Controllers;

[Authorize]
[Route("Admin")]
public class AdminController : Controller
{
    private readonly IJobPostingService _jobPostingService;
    private readonly IJobApplicationService _jobApplicationService;

    public AdminController(
        IJobPostingService jobPostingService,
        IJobApplicationService jobApplicationService)
    {
        _jobPostingService = jobPostingService;
        _jobApplicationService = jobApplicationService;
    }

    [HttpGet("")]
    public async Task<IActionResult> Index()
    {
        var employerId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrWhiteSpace(employerId))
        {
            return Forbid();
        }

        var jobPostings = await _jobPostingService.GetByEmployerIdAsync(employerId);
        var jobPostingIds = jobPostings.Select(jobPosting => jobPosting.Id).ToHashSet();
        var applications = await _jobApplicationService.GetAllAsync();

        ViewBag.Applications = applications
            .Where(application => jobPostingIds.Contains(application.JobPostingId))
            .ToList();

        return View(jobPostings);
    }

    [HttpGet("Create")]
    public IActionResult Create()
    {
        return View(new JobPosting());
    }

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
            return View(jobPosting);
        }

        jobPosting.EmployerId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? string.Empty;
        await _jobPostingService.CreateAsync(jobPosting);
        TempData["SuccessMessage"] = $"The {jobPosting.Title} job was created.";

        return RedirectToAction(nameof(Index));
    }
}
