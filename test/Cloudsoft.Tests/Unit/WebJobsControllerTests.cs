using Cloudsoft.Core.Models;
using Cloudsoft.Web.Controllers;
using Microsoft.AspNetCore.Mvc;

namespace Cloudsoft.Tests.Unit;

public class WebJobsControllerTests
{
    [Fact]
    public async Task Index_ReturnsJobPostingsViewForAnonymousBrowsing()
    {
        var job = CreateJob("job-1");
        var controller = CreateController(new FakeJobPostingService(job));

        var result = await controller.Index();

        var view = Assert.IsType<ViewResult>(result);
        Assert.Equal("JobPostings", view.ViewName);
        var model = Assert.IsAssignableFrom<IReadOnlyCollection<JobPosting>>(view.Model);
        Assert.Contains(model, item => item.Id == job.Id);
    }

    [Fact]
    public async Task Details_ReturnsViewWhenJobExists()
    {
        var job = CreateJob("job-1");
        var controller = CreateController(new FakeJobPostingService(job));

        var result = await controller.Details(job.Id);

        var view = Assert.IsType<ViewResult>(result);
        Assert.Same(job, view.Model);
    }

    [Fact]
    public async Task Details_ReturnsNotFoundWhenJobIsMissing()
    {
        var controller = CreateController(new FakeJobPostingService());

        var result = await controller.Details("missing");

        Assert.IsType<NotFoundResult>(result);
    }

    [Fact]
    public void CreateGet_ReturnsJobPostingForm()
    {
        var controller = CreateController(new FakeJobPostingService());

        var result = controller.Create();

        var view = Assert.IsType<ViewResult>(result);
        Assert.Equal("JobPosting", view.ViewName);
        Assert.IsType<JobPosting>(view.Model);
    }

    [Fact]
    public async Task CreatePost_ReturnsFormWhenDeadlineIsInThePast()
    {
        var service = new FakeJobPostingService();
        var controller = CreateController(service);
        var job = CreateJob("job-1");
        job.Deadline = DateTime.UtcNow.AddDays(-1);

        var result = await controller.Create(job);

        var view = Assert.IsType<ViewResult>(result);
        Assert.Equal("JobPosting", view.ViewName);
        Assert.False(controller.ModelState.IsValid);
        Assert.Equal(0, service.CreateCallCount);
    }

    [Fact]
    public async Task CreatePost_CreatesJobAndRedirectsToListing()
    {
        var service = new FakeJobPostingService();
        var controller = CreateController(service);
        ControllerSetup.AddTempData(controller);
        var job = CreateJob("job-1");

        var result = await controller.Create(job);

        var redirect = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal(nameof(JobsController.Index), redirect.ActionName);
        Assert.Equal(1, service.CreateCallCount);
        Assert.Contains("Thank you for posting", controller.TempData["SuccessMessage"]?.ToString());
    }

    [Fact]
    public async Task ToggleIsActive_ReturnsNotFoundWhenJobIsMissing()
    {
        var controller = CreateController(new FakeJobPostingService());
        ControllerSetup.AddTempData(controller);

        var result = await controller.ToggleIsActive("missing");

        Assert.IsType<NotFoundResult>(result);
    }

    [Fact]
    public async Task ToggleIsActive_ChangesStateAndRedirects()
    {
        var job = CreateJob("job-1");
        var service = new FakeJobPostingService(job);
        var controller = CreateController(service);
        ControllerSetup.AddTempData(controller);

        var result = await controller.ToggleIsActive(job.Id);

        var redirect = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal(nameof(JobsController.JobPostings), redirect.ActionName);
        Assert.False(job.IsActive);
        Assert.Contains("deactivated", controller.TempData["SuccessMessage"]?.ToString());
    }

    private static JobPosting CreateJob(string id)
    {
        return new JobPosting
        {
            Id = id,
            Title = "Full Stack Developer",
            Description = "Build web apps",
            Location = "Stockholm",
            EmployerId = "employer-1",
            CreatedAtUtc = DateTime.UtcNow,
            Deadline = DateTime.UtcNow.AddDays(10),
            IsActive = true
        };
    }

    private static JobsController CreateController(FakeJobPostingService jobPostingService)
    {
        return new JobsController(
            jobPostingService,
            new FakeJobApplicationService(),
            new FakeCountryLookupService());
    }
}
