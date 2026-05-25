using System.Net;
using System.Net.Http.Json;
using Cloudsoft.Api.Dtos;

namespace Cloudsoft.Tests.Integration;

public class ApiJobEndpointTests
{
    [Fact]
    public async Task GetJobs_ReturnsAllCreatedJobPostings()
    {
        await using var factory = new CloudsoftApiFactory();
        var client = factory.CreateClient();
        var job = CreateJob("api-all");

        var createResponse = await PostJobAsync(client, job);
        var created = await createResponse.Content.ReadFromJsonAsync<JobPostingDto>();
        var jobs = await client.GetFromJsonAsync<List<JobPostingDto>>("/api/jobs");

        Assert.Equal(HttpStatusCode.Created, createResponse.StatusCode);
        Assert.NotNull(created);
        Assert.NotNull(jobs);
        Assert.Contains(jobs, item => item.Id == created.Id);
    }

    [Fact]
    public async Task GetActiveJobs_ReturnsOnlyActiveJobPostings()
    {
        await using var factory = new CloudsoftApiFactory();
        var client = factory.CreateClient();
        var active = CreateJob("api-active", isActive: true);
        var inactive = CreateJob("api-inactive", isActive: false);

        await PostJobAsync(client, active);
        var inactiveResponse = await PostJobAsync(client, inactive);
        var createdInactive = await inactiveResponse.Content.ReadFromJsonAsync<JobPostingDto>();
        var jobs = await client.GetFromJsonAsync<List<JobPostingDto>>("/api/jobs/active");

        Assert.NotNull(jobs);
        Assert.Contains(jobs, item => item.Title == active.Title);
        Assert.NotNull(createdInactive);
        Assert.DoesNotContain(jobs, item => item.Id == createdInactive.Id);
    }

    [Fact]
    public async Task GetJobById_ReturnsCreatedJobPosting()
    {
        await using var factory = new CloudsoftApiFactory();
        var client = factory.CreateClient();
        var job = CreateJob("api-details");

        var createResponse = await PostJobAsync(client, job);
        var created = await createResponse.Content.ReadFromJsonAsync<JobPostingDto>();
        Assert.NotNull(created);

        var found = await client.GetFromJsonAsync<JobPostingDto>($"/api/jobs/{created.Id}");

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

        var response = await PostJobAsync(client, job);
        var created = await response.Content.ReadFromJsonAsync<JobPostingDto>();

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        Assert.NotNull(response.Headers.Location);
        Assert.NotNull(created);
        Assert.Contains($"/api/jobs/{created.Id}", response.Headers.Location.ToString());
        Assert.Equal(job.Title, created.Title);
    }

    [Fact]
    public async Task CreateJob_ReturnsUnauthorizedWithoutApiKey()
    {
        await using var factory = new CloudsoftApiFactory();
        var client = factory.CreateClient();
        var job = CreateJob("api-no-key");

        var response = await client.PostAsJsonAsync("/api/jobs", job);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task CreateJob_ReturnsUnauthorizedWithInvalidApiKey()
    {
        await using var factory = new CloudsoftApiFactory();
        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-API-Key", "wrong-key");
        var job = CreateJob("api-wrong-key");

        var response = await client.PostAsJsonAsync("/api/jobs", job);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    private static CreateJobPostingDto CreateJob(string id, bool isActive = true)
    {
        return new CreateJobPostingDto
        {
            Title = $"Integration Developer {id}",
            Description = "Build and verify services",
            Location = "Stockholm",
            Deadline = DateTime.UtcNow.AddDays(30),
            IsActive = isActive
        };
    }

    private static async Task<HttpResponseMessage> PostJobAsync(HttpClient client, CreateJobPostingDto job)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, "/api/jobs")
        {
            Content = JsonContent.Create(job)
        };
        request.Headers.Add("X-API-Key", "test-api-write-key");

        return await client.SendAsync(request);
    }
}
