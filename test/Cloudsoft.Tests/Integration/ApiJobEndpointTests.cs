using System.Net;
using System.Net.Http.Json;
using Cloudsoft.Core.Models;

namespace Cloudsoft.Tests.Integration;

public class ApiJobEndpointTests
{
    [Fact]
    public async Task GetJobs_ReturnsAllCreatedJobPostings()
    {
        await using var factory = new CloudsoftApiFactory();
        var client = factory.CreateClient();
        var job = CreateJob("api-all");

        var createResponse = await client.PostAsJsonAsync("/api/jobs", job);
        var jobs = await client.GetFromJsonAsync<List<JobPosting>>("/api/jobs");

        Assert.Equal(HttpStatusCode.Created, createResponse.StatusCode);
        Assert.NotNull(jobs);
        Assert.Contains(jobs, item => item.Id == job.Id);
    }

    [Fact]
    public async Task GetActiveJobs_ReturnsOnlyActiveJobPostings()
    {
        await using var factory = new CloudsoftApiFactory();
        var client = factory.CreateClient();
        var active = CreateJob("api-active", isActive: true);
        var inactive = CreateJob("api-inactive", isActive: false);

        await client.PostAsJsonAsync("/api/jobs", active);
        await client.PostAsJsonAsync("/api/jobs", inactive);
        var jobs = await client.GetFromJsonAsync<List<JobPosting>>("/api/jobs/active");

        Assert.NotNull(jobs);
        Assert.Contains(jobs, item => item.Id == active.Id);
        Assert.DoesNotContain(jobs, item => item.Id == inactive.Id);
    }

    [Fact]
    public async Task GetJobById_ReturnsCreatedJobPosting()
    {
        await using var factory = new CloudsoftApiFactory();
        var client = factory.CreateClient();
        var job = CreateJob("api-details");

        await client.PostAsJsonAsync("/api/jobs", job);
        var found = await client.GetFromJsonAsync<JobPosting>($"/api/jobs/{job.Id}");

        Assert.NotNull(found);
        Assert.Equal(job.Title, found.Title);
        Assert.Equal(job.Location, found.Location);
        Assert.Equal(job.Description, found.Description);
        Assert.Equal(job.Deadline.Date, found.Deadline.Date);
    }

    [Fact]
    public async Task GetJobById_ReturnsNotFoundForMissingJobPosting()
    {
        await using var factory = new CloudsoftApiFactory();
        var client = factory.CreateClient();

        var response = await client.GetAsync("/api/jobs/missing");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task CreateJob_ReturnsCreatedLocationHeader()
    {
        await using var factory = new CloudsoftApiFactory();
        var client = factory.CreateClient();
        var job = CreateJob("api-created");

        var response = await client.PostAsJsonAsync("/api/jobs", job);
        var created = await response.Content.ReadFromJsonAsync<JobPosting>();

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        Assert.NotNull(response.Headers.Location);
        Assert.Contains($"/api/jobs/{job.Id}", response.Headers.Location.ToString());
        Assert.NotNull(created);
        Assert.Equal(job.Id, created.Id);
    }

    private static JobPosting CreateJob(string id, bool isActive = true)
    {
        return new JobPosting
        {
            Id = $"{id}-{Guid.NewGuid():N}",
            Title = "Integration Developer",
            Description = "Build and verify services",
            Location = "Stockholm",
            CreatedAtUtc = DateTime.UtcNow,
            Deadline = DateTime.UtcNow.AddDays(30),
            IsActive = isActive
        };
    }
}
