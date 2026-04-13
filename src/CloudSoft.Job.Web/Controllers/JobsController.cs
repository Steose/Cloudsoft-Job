
using CloudSoft.Job.Domain.Entities;
using Microsoft.AspNetCore.Mvc;

namespace CloudSoft.Job.Web.Controllers;

public class JobsController : Controller
{
    private static List<JobPosting> _jobPostings = [];
    public IActionResult JobPosting()
    {
        return View();
    }
    [HttpPost]
    public IActionResult JobPosting(JobPosting jobPosting)
    
    {        
        Console.WriteLine($"Received job posting: Title='{jobPosting.Title}', Description='{jobPosting.Description}', Location='{jobPosting.Location}'");
        ViewBag.Message = $"Thank you for posting {jobPosting.Title} job!";
        _jobPostings.Add(jobPosting);
        return RedirectToAction(nameof(JobPosting)); 
    }
    
    [HttpGet]
    public IActionResult JobPostings()
    {
        return View(_jobPostings);
    }
}
