using System.Security.Claims;
using Cloudsoft.Core.Models;
using Cloudsoft.Core.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Cloudsoft.Web.Controllers;

[Route("Job")]
public class JobsController : Controller
{
    private readonly IJobPostingService _jobPostingService;
    private readonly IJobApplicationService _jobApplicationService;
    private readonly ICountryLookupService _countryLookupService;

    public JobsController(
        IJobPostingService jobPostingService,
        IJobApplicationService jobApplicationService,
        ICountryLookupService countryLookupService)
    {
        _jobPostingService = jobPostingService;
        _jobApplicationService = jobApplicationService;
        _countryLookupService = countryLookupService;
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

        ViewBag.Countries = await _countryLookupService.GetCountriesAsync();
        return View(jobPosting);
    }

    [HttpPost("Apply")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Apply(
        string jobPostingId,
        string fullName,
        string email,
        string countryCode,
        IFormFile? cvFile,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(jobPostingId))
        {
            return NotFound();
        }

        if (cvFile is null || cvFile.Length == 0)
        {
            TempData["ErrorMessage"] = "Please attach a CV file.";
            return RedirectToAction(nameof(Details), new { id = jobPostingId });
        }

        var application = new JobApplication
        {
            JobPostingId = jobPostingId,
            FullName = fullName,
            Email = email,
            CountryCode = countryCode
        };

        try
        {
            await using var stream = cvFile.OpenReadStream();
            await _jobApplicationService.SubmitAsync(application, stream, cvFile.FileName, cancellationToken);
            TempData["SuccessMessage"] = "Your application was submitted successfully.";
        }
        catch (InvalidOperationException ex)
        {
            TempData["ErrorMessage"] = ex.Message;
        }

        return RedirectToAction(nameof(Details), new { id = jobPostingId });
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

        jobPosting.EmployerId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? string.Empty;
        await _jobPostingService.CreateAsync(jobPosting);
        TempData["SuccessMessage"] = $"Thank you for posting the {jobPosting.Title} job!";

        return RedirectToAction(nameof(Index));
    }
}
