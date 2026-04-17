
using CloudsoftJob.Core.Entities;
using Microsoft.AspNetCore.Mvc;

namespace CloudsoftJob.Web.Controllers;

public class JobsController : Controller
{
    private static readonly List<JobPosting> _jobPostings = new();

    public IActionResult JobPosting()
    {
        return View(new JobPosting());
    }

    [HttpPost]
    public IActionResult JobPosting(JobPosting jobPosting)
    {
        if (!ModelState.IsValid)
        {
            return View(jobPosting);
        }

        jobPosting.Id ??= Guid.NewGuid().ToString();
        if (jobPosting.CreatedAtUtc == default)
        {
            jobPosting.CreatedAtUtc = DateTime.UtcNow;
        }

        _jobPostings.Add(jobPosting);
        TempData["SuccessMessage"] = $"Thank you for posting the {jobPosting.Title} job!";

        return RedirectToAction(nameof(JobPosting));
    }

    [HttpGet]
    public IActionResult JobPostings()
    {
        return View(_jobPostings);
    }

    [HttpPost]
    public IActionResult ToggleIsActive(string id)
    {
        var jobPosting = _jobPostings.Find(j => j.Id == id);
        if (jobPosting == null)
        {
            return NotFound();
        }

        jobPosting.IsActive = !jobPosting.IsActive;
        TempData["SuccessMessage"] = jobPosting.IsActive
            ? $"Job '{jobPosting.Title}' is now active."
            : $"Job '{jobPosting.Title}' has been deactivated.";

        return RedirectToAction(nameof(JobPostings));
    }
}
