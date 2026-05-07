using Cloudsoft.Core.Models;
using Cloudsoft.Core.Services.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewFeatures;

namespace Cloudsoft.Tests.Unit;

internal sealed class FakeJobPostingService : IJobPostingService
{
    private readonly Dictionary<string, JobPosting> _jobs = new();

    public int CreateCallCount { get; private set; }

    public FakeJobPostingService(params JobPosting[] jobs)
    {
        foreach (var job in jobs)
        {
            _jobs[job.Id] = job;
        }
    }

    public Task<IReadOnlyCollection<JobPosting>> GetAllAsync()
    {
        return Task.FromResult<IReadOnlyCollection<JobPosting>>(_jobs.Values.ToList());
    }

    public Task<IReadOnlyCollection<JobPosting>> GetActiveAsync()
    {
        return Task.FromResult<IReadOnlyCollection<JobPosting>>(_jobs.Values.Where(job => job.IsActive).ToList());
    }

    public Task<JobPosting?> GetByIdAsync(string id)
    {
        return Task.FromResult(_jobs.TryGetValue(id, out var job) ? job : null);
    }

    public Task<JobPosting> CreateAsync(JobPosting jobPosting)
    {
        CreateCallCount++;
        _jobs[jobPosting.Id] = jobPosting;
        return Task.FromResult(jobPosting);
    }

    public Task<bool> UpdateAsync(JobPosting jobPosting)
    {
        if (!_jobs.ContainsKey(jobPosting.Id))
        {
            return Task.FromResult(false);
        }

        _jobs[jobPosting.Id] = jobPosting;
        return Task.FromResult(true);
    }

    public Task<bool> DeleteAsync(string id)
    {
        return Task.FromResult(_jobs.Remove(id));
    }

    public Task<bool> ToggleIsActiveAsync(string id)
    {
        if (!_jobs.TryGetValue(id, out var job))
        {
            return Task.FromResult(false);
        }

        job.IsActive = !job.IsActive;
        return Task.FromResult(true);
    }
}

internal sealed class NullTempDataProvider : ITempDataProvider
{
    public IDictionary<string, object> LoadTempData(HttpContext context)
    {
        return new Dictionary<string, object>();
    }

    public void SaveTempData(HttpContext context, IDictionary<string, object> values)
    {
    }
}

internal static class ControllerSetup
{
    public static void AddTempData(Controller controller)
    {
        controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext()
        };
        controller.TempData = new TempDataDictionary(controller.HttpContext, new NullTempDataProvider());
    }
}
