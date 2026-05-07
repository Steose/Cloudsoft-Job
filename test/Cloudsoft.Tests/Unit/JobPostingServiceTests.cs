using Cloudsoft.Core.Data;
using Cloudsoft.Core.Models;
using Cloudsoft.Core.Repositories;
using Cloudsoft.Core.Services;

namespace Cloudsoft.Tests.Unit;

public class JobPostingServiceTests
{
    private readonly JobPostingService _service = new(new JobPostingRepository(new InMemoryDatabase()));

    [Fact]
    public async Task CreateAsync_AddsIdAndCreatedDateWhenMissing()
    {
        var job = new JobPosting
        {
            Id = "",
            Title = "Cloud Engineer",
            Description = "Run cloud workloads",
            Location = "Remote",
            CreatedAtUtc = default,
            Deadline = DateTime.UtcNow.AddDays(30),
            IsActive = true
        };

        var created = await _service.CreateAsync(job);

        Assert.False(string.IsNullOrWhiteSpace(created.Id));
        Assert.NotEqual(default, created.CreatedAtUtc);
    }

    [Fact]
    public async Task ToggleIsActiveAsync_ChangesActiveState()
    {
        var created = await _service.CreateAsync(CreateJob("toggle", isActive: true));

        var toggled = await _service.ToggleIsActiveAsync(created.Id);
        var found = await _service.GetByIdAsync(created.Id);

        Assert.True(toggled);
        Assert.NotNull(found);
        Assert.False(found.IsActive);
    }

    [Fact]
    public async Task ToggleIsActiveAsync_ReturnsFalseWhenJobIsMissing()
    {
        var toggled = await _service.ToggleIsActiveAsync("missing");

        Assert.False(toggled);
    }

    [Fact]
    public async Task UpdateAndDeleteAsync_PerformCrudOperations()
    {
        var created = await _service.CreateAsync(CreateJob("crud"));
        created.Title = "Updated title";

        var updated = await _service.UpdateAsync(created);
        var deleted = await _service.DeleteAsync(created.Id);
        var found = await _service.GetByIdAsync(created.Id);

        Assert.True(updated);
        Assert.True(deleted);
        Assert.Null(found);
    }

    private static JobPosting CreateJob(string id, bool isActive = true)
    {
        return new JobPosting
        {
            Id = id,
            Title = "Developer",
            Description = "Build software",
            Location = "Malmo",
            CreatedAtUtc = DateTime.UtcNow,
            Deadline = DateTime.UtcNow.AddDays(20),
            IsActive = isActive
        };
    }
}
