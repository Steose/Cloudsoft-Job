using Cloudsoft.Core.Models;

namespace Cloudsoft.Core.Services.Interfaces;

public interface IEmployerAuthenticationService
{
    Task<EmployerUser?> ValidateCredentialsAsync(string email, string password);

    Task<EmployerUser?> RegisterAsync(string email, string password, string displayName);

    Task<bool> EmailExistsAsync(string email);
}
