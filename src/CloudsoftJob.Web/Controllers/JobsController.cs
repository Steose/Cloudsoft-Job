
using CloudsoftJob.Core.Models;
using CloudsoftJob.Core.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CloudsoftJob.Web.Controllers;

[Route("Job")]
public class JobsController : Controller
{
    private readonly IJobPostingService _jobPostingService;

    public JobsController(IJobPostingService jobPostingService)
    {
        _jobPostingService = jobPostingService;
    }

    [HttpGet("")]
    public IActionResult Index()
    {
        return View("JobPostings", _jobPostingService.GetAll());
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
    public IActionResult Details(string id)
    {
        var jobPosting = _jobPostingService.GetById(id);
        if (jobPosting == null)
        {
            return NotFound();
        }

        return View(jobPosting);
    }

    [Authorize]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult ToggleIsActive(string id)
    {
        var jobPosting = _jobPostingService.GetById(id);
        if (jobPosting == null || !_jobPostingService.ToggleIsActive(id))
        {
            return NotFound();
        }

        TempData["SuccessMessage"] = jobPosting.IsActive
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
    public IActionResult Create(JobPosting jobPosting)
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

        _jobPostingService.Create(jobPosting);
        TempData["SuccessMessage"] = $"Thank you for posting the {jobPosting.Title} job!";

        return RedirectToAction(nameof(Index));
    }
}
