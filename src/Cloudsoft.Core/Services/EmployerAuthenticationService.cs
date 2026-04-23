using Cloudsoft.Core.Models;
using Cloudsoft.Core.Repositories.Interfaces;
using Cloudsoft.Core.Services.Interfaces;

namespace Cloudsoft.Core.Services;

public class EmployerAuthenticationService : IEmployerAuthenticationService
{
    private readonly IEmployerRepository _employerRepository;

    public EmployerAuthenticationService(IEmployerRepository employerRepository)
    {
        _employerRepository = employerRepository;
    }

    public async Task<EmployerUser?> ValidateCredentialsAsync(string email, string password)
    {
        var employerAccount = await _employerRepository.GetByEmailAsync(email);
        if (employerAccount == null || employerAccount.Password != password)
        {
            return null;
        }

        return employerAccount.User;
    }

    public async Task<EmployerUser?> RegisterAsync(string email, string password, string displayName)
    {
        if (string.IsNullOrWhiteSpace(email) ||
            string.IsNullOrWhiteSpace(password) ||
            string.IsNullOrWhiteSpace(displayName))
        {
            return null;
        }

        var employerAccount = new EmployerAccount
        {
            User = new EmployerUser
            {
                Id = Guid.NewGuid().ToString(),
                Email = email.Trim(),
                DisplayName = displayName.Trim()
            },
            Password = password,
            NormalizedEmail = email.Trim().ToUpperInvariant()
        };

        var registeredEmployerAccount = await _employerRepository.AddAsync(employerAccount);

        return registeredEmployerAccount?.User;
    }

    public Task<bool> EmailExistsAsync(string email)
    {
        return _employerRepository.EmailExistsAsync(email);
    }
}
