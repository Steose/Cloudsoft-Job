using Cloudsoft.Api.Controllers;
using Cloudsoft.Api.Dtos;
using Cloudsoft.Core.Models;
using Microsoft.AspNetCore.Mvc;

namespace Cloudsoft.Tests.Unit;

public class ApiJobsControllerTests
{
    [Fact]
    public async Task GetAll_ReturnsAllJobPostings()
    {
        var active = CreateJob("active", true);
        var inactive = CreateJob("inactive", false);
        var controller = new JobsController(new FakeJobPostingService(active, inactive));

        var jobs = await controller.GetAll();

        Assert.Equal(2, jobs.Count);
    }

    [Fact]
    public async Task GetActive_ReturnsOnlyActiveJobPostings()
    {
        var active = CreateJob("active", true);
        var inactive = CreateJob("inactive", false);
        var controller = new JobsController(new FakeJobPostingService(active, inactive));

        var jobs = await controller.GetActive();

        var job = Assert.Single(jobs);
        Assert.Equal(active.Id, job.Id);
    }

    [Fact]
    public async Task GetById_ReturnsOkWhenJobExists()
    {
        var job = CreateJob("job-1", true);
        var controller = new JobsController(new FakeJobPostingService(job));

        var result = await controller.GetById(job.Id);

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var model = Assert.IsType<JobPostingDto>(ok.Value);
        Assert.Equal(job.Id, model.Id);
    }

    [Fact]
    public async Task GetById_ReturnsNotFoundWhenJobIsMissing()
    {
        var controller = new JobsController(new FakeJobPostingService());

        var result = await controller.GetById("missing");

        Assert.IsType<NotFoundResult>(result.Result);
    }

    [Fact]
    public async Task Create_ReturnsCreatedResponse()
    {
        var job = CreateJob("job-1", true);
        var dto = new CreateJobPostingDto
        {
            Title = job.Title,
            Description = job.Description,
            Location = job.Location,
            Deadline = job.Deadline,
            IsActive = job.IsActive
        };
        var controller = new JobsController(new FakeJobPostingService());

        var result = await controller.Create(dto);

        var created = Assert.IsType<CreatedAtActionResult>(result.Result);
        Assert.Equal(nameof(JobsController.GetById), created.ActionName);
        var model = Assert.IsType<JobPostingDto>(created.Value);
        Assert.Equal(job.Title, model.Title);
        Assert.Equal(job.Description, model.Description);
        Assert.Equal(job.Location, model.Location);
        Assert.Equal(job.Deadline, model.Deadline);
        Assert.Equal(job.IsActive, model.IsActive);
    }

    private static JobPosting CreateJob(string id, bool isActive)
    {
        return new JobPosting
        {
            Id = id,
            Title = "Platform Engineer",
            Description = "Run platform services",
            Location = "Remote",
            EmployerId = "employer-1",
            CreatedAtUtc = DateTime.UtcNow,
            Deadline = DateTime.UtcNow.AddDays(21),
            IsActive = isActive
        };
    }
}
