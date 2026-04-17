using CloudsoftJob.Core.Models;
using CloudsoftJob.Core.Services.Interfaces;

namespace CloudsoftJob.Core.Services;

public class EmployerAuthenticationService : IEmployerAuthenticationService
{
    private const string EmployerEmail = "employer@cloudsoft.com";
    private const string EmployerPassword = "Password123!";
    private readonly List<EmployerAccount> _employers =
    [
        new(
            new EmployerUser
            {
                Id = "cloudsoft-employer",
                Email = EmployerEmail,
                DisplayName = "CloudSoft Employer"
            },
            EmployerPassword)
    ];
    private readonly object _syncRoot = new();

    public EmployerUser? ValidateCredentials(string email, string password)
    {
        lock (_syncRoot)
        {
            var employer = _employers.FirstOrDefault(account =>
                string.Equals(account.User.Email, email, StringComparison.OrdinalIgnoreCase) &&
                account.Password == password);

            return employer?.User;
        }
    }

    public EmployerUser? Register(string email, string password, string displayName)
    {
        if (string.IsNullOrWhiteSpace(email) ||
            string.IsNullOrWhiteSpace(password) ||
            string.IsNullOrWhiteSpace(displayName))
        {
            return null;
        }

        lock (_syncRoot)
        {
            if (EmailExistsCore(email))
            {
                return null;
            }

            var employer = new EmployerUser
            {
                Id = Guid.NewGuid().ToString(),
                Email = email.Trim(),
                DisplayName = displayName.Trim()
            };

            _employers.Add(new EmployerAccount(employer, password));

            return employer;
        }
    }

    public bool EmailExists(string email)
    {
        if (string.IsNullOrWhiteSpace(email))
        {
            return false;
        }

        lock (_syncRoot)
        {
            return EmailExistsCore(email);
        }
    }

    private bool EmailExistsCore(string email)
    {
        return _employers.Any(account =>
            string.Equals(account.User.Email, email, StringComparison.OrdinalIgnoreCase));
    }

    private sealed record EmployerAccount(EmployerUser User, string Password);
}
