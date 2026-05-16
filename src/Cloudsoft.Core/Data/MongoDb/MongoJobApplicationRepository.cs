using Cloudsoft.Core.Models;
using Cloudsoft.Core.Options;
using Cloudsoft.Core.Repositories.Interfaces;
using Microsoft.Extensions.Options;
using MongoDB.Bson;
using MongoDB.Driver;
using System.Text.RegularExpressions;

namespace Cloudsoft.Core.Repositories;

public class MongoJobApplicationRepository : IJobApplicationRepository
{
    private readonly IMongoCollection<JobApplication> _applications;

    public MongoJobApplicationRepository(IOptions<MongoDbOptions> options)
    {
        MongoDbMappings.Register();

        var mongoDbOptions = options.Value;
        var client = new MongoClient(mongoDbOptions.ConnectionString);
        var database = client.GetDatabase(mongoDbOptions.DatabaseName);

        _applications = database.GetCollection<JobApplication>(mongoDbOptions.JobApplicationsCollectionName);
    }

    public async Task<IReadOnlyCollection<JobApplication>> GetAllAsync()
    {
        var applications = await _applications.Find(_ => true).ToListAsync();
        return applications.OrderByDescending(application => application.SubmittedAtUtc).ToList();
    }

    public async Task<IReadOnlyCollection<JobApplication>> GetByJobPostingIdAsync(string jobPostingId)
    {
        var applications = await _applications
            .Find(application => application.JobPostingId == jobPostingId)
            .ToListAsync();

        return applications.OrderByDescending(application => application.SubmittedAtUtc).ToList();
    }

    public async Task<bool> ExistsForJobAndEmailAsync(string jobPostingId, string email)
    {
        var emailPattern = $"^{Regex.Escape(email.Trim())}$";
        var filter = Builders<JobApplication>.Filter.And(
            Builders<JobApplication>.Filter.Eq(application => application.JobPostingId, jobPostingId),
            Builders<JobApplication>.Filter.Regex(
                application => application.Email,
                new BsonRegularExpression(emailPattern, "i")));

        return await _applications.Find(filter).AnyAsync();
    }

    public async Task<JobApplication> AddAsync(JobApplication application)
    {
        ArgumentNullException.ThrowIfNull(application);

        await _applications.InsertOneAsync(application);
        return application;
    }
}
