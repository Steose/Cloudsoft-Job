using CloudsoftJob.Core.Data;
using CloudsoftJob.Core.Models;
using CloudsoftJob.Core.Repositories.Interfaces;

namespace CloudsoftJob.Core.Repositories;

public class EmployerRepository : IEmployerRepository
{
    private readonly IInMemoryDatabase _database;

    public EmployerRepository(IInMemoryDatabase database)
    {
        _database = database;
    }

    public Task<EmployerAccount?> GetByEmailAsync(string email)
    {
        if (string.IsNullOrWhiteSpace(email))
        {
            return Task.FromResult<EmployerAccount?>(null);
        }

        return Task.FromResult(
            _database.Employers.TryGetValue(NormalizeEmail(email), out var employerAccount)
                ? Clone(employerAccount)
                : null);
    }

    public Task<bool> EmailExistsAsync(string email)
    {
        if (string.IsNullOrWhiteSpace(email))
        {
            return Task.FromResult(false);
        }

        return Task.FromResult(_database.Employers.ContainsKey(NormalizeEmail(email)));
    }

    public Task<EmployerAccount?> AddAsync(EmployerAccount employerAccount)
    {
        ArgumentNullException.ThrowIfNull(employerAccount);

        if (string.IsNullOrWhiteSpace(employerAccount.User.Email))
        {
            return Task.FromResult<EmployerAccount?>(null);
        }

        var storedEmployerAccount = Clone(employerAccount);
        storedEmployerAccount.NormalizedEmail = NormalizeEmail(storedEmployerAccount.User.Email);
        var added = _database.Employers.TryAdd(
            storedEmployerAccount.NormalizedEmail,
            storedEmployerAccount);

        return Task.FromResult(added ? Clone(storedEmployerAccount) : null);
    }

    private static string NormalizeEmail(string email)
    {
        return email.Trim().ToUpperInvariant();
    }

    private static EmployerAccount Clone(EmployerAccount employerAccount)
    {
        return new EmployerAccount
        {
            User = new EmployerUser
            {
                Id = employerAccount.User.Id,
                Email = employerAccount.User.Email,
                DisplayName = employerAccount.User.DisplayName
            },
            Password = employerAccount.Password,
            NormalizedEmail = employerAccount.NormalizedEmail
        };
    }
}
