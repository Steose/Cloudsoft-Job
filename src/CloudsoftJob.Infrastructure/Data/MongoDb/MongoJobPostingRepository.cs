using CloudsoftJob.Core.Models;
using CloudsoftJob.Core.Options;
using CloudsoftJob.Core.Repositories.Interfaces;
using Microsoft.Extensions.Options;
using MongoDB.Driver;

namespace CloudsoftJob.Core.Repositories;

public class MongoJobPostingRepository : IJobPostingRepository
{
    private readonly IMongoCollection<JobPosting> _jobPostings;

    public MongoJobPostingRepository(IOptions<MongoDbOptions> options)
    {
        MongoDbMappings.Register();

        var mongoDbOptions = options.Value;
        var client = new MongoClient(mongoDbOptions.ConnectionString);
        var database = client.GetDatabase(mongoDbOptions.DatabaseName);

        _jobPostings = database.GetCollection<JobPosting>(mongoDbOptions.JobPostingsCollectionName);
    }

    public async Task<IReadOnlyCollection<JobPosting>> GetAllAsync()
    {
        return await _jobPostings
            .Find(_ => true)
            .SortByDescending(jobPosting => jobPosting.CreatedAtUtc)
            .ToListAsync();
    }

    public async Task<IReadOnlyCollection<JobPosting>> GetActiveAsync()
    {
        return await _jobPostings
            .Find(jobPosting => jobPosting.IsActive)
            .SortByDescending(jobPosting => jobPosting.CreatedAtUtc)
            .ToListAsync();
    }

    public async Task<JobPosting?> GetByIdAsync(string id)
    {
        if (string.IsNullOrWhiteSpace(id))
        {
            return null;
        }

        return await _jobPostings
            .Find(jobPosting => jobPosting.Id == id)
            .FirstOrDefaultAsync();
    }

    public async Task<JobPosting> AddAsync(JobPosting jobPosting)
    {
        ArgumentNullException.ThrowIfNull(jobPosting);

        await _jobPostings.InsertOneAsync(jobPosting);

        return jobPosting;
    }

    public async Task<bool> UpdateAsync(JobPosting jobPosting)
    {
        ArgumentNullException.ThrowIfNull(jobPosting);

        if (string.IsNullOrWhiteSpace(jobPosting.Id))
        {
            return false;
        }

        var result = await _jobPostings.ReplaceOneAsync(
            existing => existing.Id == jobPosting.Id,
            jobPosting);

        return result.ModifiedCount == 1;
    }

    public async Task<bool> DeleteAsync(string id)
    {
        if (string.IsNullOrWhiteSpace(id))
        {
            return false;
        }

        var result = await _jobPostings.DeleteOneAsync(jobPosting => jobPosting.Id == id);

        return result.DeletedCount == 1;
    }
}
