using CloudsoftJob.Core.Models;

namespace CloudsoftJob.Core.Services.Interfaces;

public interface IEmployerAuthenticationService
{
    Task<EmployerUser?> ValidateCredentialsAsync(string email, string password);

    Task<EmployerUser?> RegisterAsync(string email, string password, string displayName);

    Task<bool> EmailExistsAsync(string email);
}
