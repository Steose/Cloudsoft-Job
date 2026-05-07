using Cloudsoft.Core.Data;
using Cloudsoft.Core.Models;
using Cloudsoft.Core.Repositories;

namespace Cloudsoft.Tests.Unit;

public class JobPostingRepositoryTests
{
    private readonly JobPostingRepository _repository = new(new InMemoryDatabase());

    [Fact]
    public async Task AddAndGetByIdAsync_CreatesAndReadsJobPosting()
    {
        var job = CreateJob("job-1");

        var created = await _repository.AddAsync(job);
        var found = await _repository.GetByIdAsync(created.Id);

        Assert.NotNull(found);
        Assert.Equal("Backend Developer", found.Title);
        Assert.Equal("Stockholm", found.Location);
        Assert.Equal("Build APIs", found.Description);
        Assert.True(found.IsActive);
    }

    [Fact]
    public async Task GetAllAsync_ReturnsNewestFirst()
    {
        await _repository.AddAsync(CreateJob("old", createdAtUtc: DateTime.UtcNow.AddDays(-2)));
        await _repository.AddAsync(CreateJob("new", createdAtUtc: DateTime.UtcNow));

        var jobs = await _repository.GetAllAsync();

        Assert.Collection(
            jobs,
            job => Assert.Equal("new", job.Id),
            job => Assert.Equal("old", job.Id));
    }

    [Fact]
    public async Task GetActiveAsync_ReturnsOnlyActiveJobs()
    {
        await _repository.AddAsync(CreateJob("active", isActive: true));
        await _repository.AddAsync(CreateJob("inactive", isActive: false));

        var jobs = await _repository.GetActiveAsync();

        var job = Assert.Single(jobs);
        Assert.Equal("active", job.Id);
    }

    [Fact]
    public async Task UpdateAsync_UpdatesExistingJobPosting()
    {
        var created = await _repository.AddAsync(CreateJob("job-1"));
        created.Title = "Updated Developer";
        created.Description = "Updated description";
        created.Location = "Gothenburg";
        created.IsActive = false;

        var updated = await _repository.UpdateAsync(created);
        var found = await _repository.GetByIdAsync(created.Id);

        Assert.True(updated);
        Assert.NotNull(found);
        Assert.Equal("Updated Developer", found.Title);
        Assert.Equal("Updated description", found.Description);
        Assert.Equal("Gothenburg", found.Location);
        Assert.False(found.IsActive);
    }

    [Fact]
    public async Task UpdateAsync_ReturnsFalseForMissingJobPosting()
    {
        var updated = await _repository.UpdateAsync(CreateJob("missing"));

        Assert.False(updated);
    }

    [Fact]
    public async Task DeleteAsync_RemovesExistingJobPosting()
    {
        var created = await _repository.AddAsync(CreateJob("job-1"));

        var deleted = await _repository.DeleteAsync(created.Id);
        var found = await _repository.GetByIdAsync(created.Id);

        Assert.True(deleted);
        Assert.Null(found);
    }

    [Fact]
    public async Task DeleteAsync_ReturnsFalseForMissingJobPosting()
    {
        var deleted = await _repository.DeleteAsync("missing");

        Assert.False(deleted);
    }

    private static JobPosting CreateJob(
        string id,
        bool isActive = true,
        DateTime? createdAtUtc = null)
    {
        return new JobPosting
        {
            Id = id,
            Title = "Backend Developer",
            Description = "Build APIs",
            Location = "Stockholm",
            CreatedAtUtc = createdAtUtc ?? DateTime.UtcNow,
            Deadline = DateTime.UtcNow.AddDays(14),
            IsActive = isActive
        };
    }
}
