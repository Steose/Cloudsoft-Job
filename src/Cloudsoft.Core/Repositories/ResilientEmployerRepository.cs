using Cloudsoft.Core.Data;
using Cloudsoft.Core.Models;
using Cloudsoft.Core.Repositories.Interfaces;
using Microsoft.Extensions.Logging;

namespace Cloudsoft.Core.Repositories;

public class ResilientEmployerRepository : IEmployerRepository
{
    private readonly MongoEmployerRepository _mongoRepository;
    private readonly EmployerRepository _fallbackRepository;
    private readonly IInMemoryDatabase _database;
    private readonly ILogger<ResilientEmployerRepository> _logger;

    public ResilientEmployerRepository(
        MongoEmployerRepository mongoRepository,
        EmployerRepository fallbackRepository,
        IInMemoryDatabase database,
        ILogger<ResilientEmployerRepository> logger)
    {
        _mongoRepository = mongoRepository;
        _fallbackRepository = fallbackRepository;
        _database = database;
        _logger = logger;
    }

    public async Task<EmployerAccount?> GetByEmailAsync(string email)
    {
        try
        {
            var employerAccount = await _mongoRepository.GetByEmailAsync(email);
            if (employerAccount != null)
            {
                _database.Employers[NormalizeEmail(employerAccount.User.Email)] = Clone(employerAccount);
            }

            return employerAccount;
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "MongoDb employer repository failed during {Operation}. Falling back to in-memory storage.",
                nameof(GetByEmailAsync));

            return await _fallbackRepository.GetByEmailAsync(email);
        }
    }

    public async Task<bool> EmailExistsAsync(string email)
    {
        try
        {
            return await _mongoRepository.EmailExistsAsync(email);
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "MongoDb employer repository failed during {Operation}. Falling back to in-memory storage.",
                nameof(EmailExistsAsync));

            return await _fallbackRepository.EmailExistsAsync(email);
        }
    }

    public async Task<EmployerAccount?> AddAsync(EmployerAccount employerAccount)
    {
        try
        {
            var addedEmployerAccount = await _mongoRepository.AddAsync(employerAccount);
            if (addedEmployerAccount != null)
            {
                _database.Employers[NormalizeEmail(addedEmployerAccount.User.Email)] = Clone(addedEmployerAccount);
            }

            return addedEmployerAccount;
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "MongoDb employer repository failed during {Operation}. Falling back to in-memory storage.",
                nameof(AddAsync));

            return await _fallbackRepository.AddAsync(employerAccount);
        }
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
