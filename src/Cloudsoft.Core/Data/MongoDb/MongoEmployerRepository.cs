using Cloudsoft.Core.Models;
using Cloudsoft.Core.Options;
using Cloudsoft.Core.Repositories.Interfaces;
using Microsoft.Extensions.Options;
using MongoDB.Driver;

namespace Cloudsoft.Core.Repositories;

public class MongoEmployerRepository : IEmployerRepository
{
    private readonly IMongoCollection<EmployerAccount> _employers;

    public MongoEmployerRepository(IOptions<MongoDbOptions> options)
    {
        MongoDbMappings.Register();

        var mongoDbOptions = options.Value;
        var client = new MongoClient(mongoDbOptions.ConnectionString);
        var database = client.GetDatabase(mongoDbOptions.DatabaseName);

        _employers = database.GetCollection<EmployerAccount>(mongoDbOptions.EmployersCollectionName);
    }

    public async Task<EmployerAccount?> GetByEmailAsync(string email)
    {
        if (string.IsNullOrWhiteSpace(email))
        {
            return null;
        }

        return await _employers
            .Find(employer => employer.NormalizedEmail == NormalizeEmail(email))
            .FirstOrDefaultAsync();
    }

    public async Task<bool> EmailExistsAsync(string email)
    {
        if (string.IsNullOrWhiteSpace(email))
        {
            return false;
        }

        return await _employers
            .Find(employer => employer.NormalizedEmail == NormalizeEmail(email))
            .AnyAsync();
    }

    public async Task<EmployerAccount?> AddAsync(EmployerAccount employerAccount)
    {
        ArgumentNullException.ThrowIfNull(employerAccount);

        if (string.IsNullOrWhiteSpace(employerAccount.User.Email))
        {
            return null;
        }

        employerAccount.NormalizedEmail = NormalizeEmail(employerAccount.User.Email);
        if (await EmailExistsAsync(employerAccount.User.Email))
        {
            return null;
        }

        await _employers.InsertOneAsync(employerAccount);

        return employerAccount;
    }

    private static string NormalizeEmail(string email)
    {
        return email.Trim().ToUpperInvariant();
    }
}
