using CloudsoftJob.Core.Models;

namespace CloudsoftJob.Core.Services.Interfaces;

public interface IEmployerAuthenticationService
{
    EmployerUser? ValidateCredentials(string email, string password);

    EmployerUser? Register(string email, string password, string displayName);

    bool EmailExists(string email);
}
